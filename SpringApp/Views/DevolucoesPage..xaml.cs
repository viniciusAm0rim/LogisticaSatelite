using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class DevolucoesPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private Devolucao? _devolucaoAtual;
    private readonly Dictionary<string, int> _contagemBipada = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ClassificacaoItemUi> _itensClassificacaoUi = new();

    private string _usuarioLogado => Preferences.Get("UsuarioNome", "Operador");

    public DevolucoesPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDevolucoesAsync();
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CarregarDevolucoesAsync();
        DevolucoesRefreshView.IsRefreshing = false;
    }

    private async Task CarregarDevolucoesAsync()
    {
        var lista = await _pedidoService.GetDevolucoesAtivasAsync();
        DevolucoesCollectionView.ItemsSource = lista;
    }

    private void OnAbrirEtapaDevolucaoClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Devolucao dev)
        {
            _devolucaoAtual = dev;

            if (dev.Status.Equals("Autorizada", StringComparison.OrdinalIgnoreCase))
            {
                LabelDocEsperado.Text = dev.EmiteNotaFiscal
                    ? $"Cliente com NF — Bipe a NF: {dev.NumeroNotaDevolucao}"
                    : $"Cliente Físico (sem NF) — Bipe o código: {dev.CodigoAutorizacao}";
                CodigoDocumentoEntry.Text = string.Empty;
                VolumesRecebidosEntry.Text = dev.VolumesInformados.ToString();
                OverlayVolumes.IsVisible = true;
            }
            else if (dev.Status.Equals("Volumes Conferidos", StringComparison.OrdinalIgnoreCase))
            {
                AbrirModalBipagemItens(dev);
            }
            else if (dev.Status.Equals("Em Classificação", StringComparison.OrdinalIgnoreCase))
            {
                AbrirModalClassificacao(dev);
            }
        }
    }

    private async void OnConfirmarVolumesClicked(object sender, EventArgs e)
    {
        if (_devolucaoAtual == null) return;

        string codigoLido = CodigoDocumentoEntry.Text?.Trim() ?? string.Empty;
        if (!int.TryParse(VolumesRecebidosEntry.Text?.Trim(), out int volumes) || volumes <= 0)
        {
            await DisplayAlert("Atenção", "Informe a quantidade de volumes recebidos.", "OK");
            return;
        }

        var (sucesso, msg, devAtualizada) = await _pedidoService.ConferirVolumesDevolucaoAsync(
            _devolucaoAtual.Id, codigoLido, volumes, _usuarioLogado);

        if (!sucesso || devAtualizada == null)
        {
            // Apenas avisa sem recusar automaticamente; o operador pode corrigir ou clicar em Recusar
            await DisplayAlert("Código Não Confere", msg, "OK");
            return;
        }

        OverlayVolumes.IsVisible = false;
        _devolucaoAtual = devAtualizada;
        await CarregarDevolucoesAsync();

        AbrirModalBipagemItens(devAtualizada);
    }

    private async void OnRecusarDevolucaoClicked(object sender, EventArgs e)
    {
        if (_devolucaoAtual == null) return;

        string motivo = await DisplayPromptAsync(
            "Recusar Devolução na Doca",
            "Informe o motivo da recusa (ex: Código divergente, itens faltando, caixa violada):",
            accept: "Confirmar Recusa",
            cancel: "Voltar",
            placeholder: "Digite o motivo...");

        if (string.IsNullOrWhiteSpace(motivo))
            return;

        var (sucesso, msg) = await _pedidoService.RecusarDevolucaoAsync(_devolucaoAtual.Id, motivo, _usuarioLogado);
        if (!sucesso)
        {
            await DisplayAlert("Erro", msg, "OK");
            return;
        }

        OverlayVolumes.IsVisible = false;
        OverlayBipagemItens.IsVisible = false;
        await DisplayAlert("Devolução Recusada", "A devolução foi marcada como 'Recusada na Doca' e informada ao Comercial.", "OK");
        await CarregarDevolucoesAsync();
    }

    private void AbrirModalBipagemItens(Devolucao dev)
    {
        _contagemBipada.Clear();
        foreach (var item in dev.Itens)
        {
            _contagemBipada[item.CodItem] = 0;
        }

        BiparSkuDevolucaoEntry.Text = string.Empty;
        AtualizarListaVisualContagem();
        OverlayBipagemItens.IsVisible = true;
    }

    private void OnZerarContagemItensClicked(object sender, EventArgs e)
    {
        if (_devolucaoAtual == null) return;
        foreach (var item in _devolucaoAtual.Itens)
        {
            _contagemBipada[item.CodItem] = 0;
        }
        AtualizarListaVisualContagem();
    }

    private async void OnBiparItemDevolucaoCompleted(object sender, EventArgs e)
    {
        if (_devolucaoAtual == null) return;

        string sku = BiparSkuDevolucaoEntry.Text?.Trim() ?? string.Empty;
        BiparSkuDevolucaoEntry.Text = string.Empty;

        if (string.IsNullOrEmpty(sku)) return;

        if (!_contagemBipada.ContainsKey(sku))
        {
            await DisplayAlert("SKU não consta no formulário", $"O código '{sku}' não faz parte desta solicitação.", "OK");
            return;
        }

        _contagemBipada[sku]++;
        AtualizarListaVisualContagem();
    }

    private void AtualizarListaVisualContagem()
    {
        ListaContagemItensStack.Children.Clear();
        if (_devolucaoAtual == null) return;

        foreach (var item in _devolucaoAtual.Itens)
        {
            int bipado = _contagemBipada.TryGetValue(item.CodItem, out int qtd) ? qtd : 0;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                Padding = new Thickness(10, 8),
                BackgroundColor = Color.FromArgb("#f8fafc")
            };

            grid.Add(new Label
            {
                Text = $"{item.CodItem} — {item.Nome}",
                FontSize = 12,
                TextColor = Color.FromArgb("#0f172a"),
                VerticalOptions = LayoutOptions.Center
            }, 0, 0);

            grid.Add(new Label
            {
                Text = $"Bipado: {bipado}",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2563eb"),
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            ListaContagemItensStack.Children.Add(grid);
        }
    }

    private async void OnValidarContagemItensClicked(object sender, EventArgs e)
    {
        if (_devolucaoAtual == null) return;

        var listaBipados = _contagemBipada
            .Select(kv => new ItemBipadoDevolucaoInput { CodItem = kv.Key, QuantidadeBipada = kv.Value })
            .ToList();

        var (sucesso, aprovadaNaConferencia, msg, devAtualizada) =
            await _pedidoService.ValidarItensDevolucaoAsync(_devolucaoAtual.Id, listaBipados, _usuarioLogado);

        if (!sucesso)
        {
            await DisplayAlert("Erro", msg, "OK");
            return;
        }

        if (!aprovadaNaConferencia)
        {
            // Mantem o modal aberto para o operador corrigir a contagem ou clicar em Recusar
            await DisplayAlert("Contagem Divergente", msg, "OK");
            return;
        }

        OverlayBipagemItens.IsVisible = false;
        await CarregarDevolucoesAsync();

        if (devAtualizada != null)
        {
            _devolucaoAtual = devAtualizada;
            AbrirModalClassificacao(devAtualizada);
        }
    }

    private void AbrirModalClassificacao(Devolucao dev)
    {
        ListaClassificacaoStack.Children.Clear();
        _itensClassificacaoUi.Clear();

        foreach (var item in dev.Itens)
        {
            var entryBom = new Entry
            {
                Text = item.QuantidadeSolicitada.ToString(),
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#0f172a"),
                WidthRequest = 65
            };

            var entryAvariado = new Entry
            {
                Text = "0",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#dc2626"),
                WidthRequest = 65
            };

            var entryMotivo = new Entry
            {
                Placeholder = "Motivo da avaria (se houver)...",
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#0f172a"),
                FontSize = 12
            };

            _itensClassificacaoUi.Add(new ClassificacaoItemUi
            {
                CodItem = item.CodItem,
                Nome = item.Nome,
                Total = item.QuantidadeSolicitada,
                EntryBom = entryBom,
                EntryAvariado = entryAvariado,
                EntryMotivo = entryMotivo
            });

            var box = new Border
            {
                Stroke = Color.FromArgb("#e2e8f0"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#f8fafc"),
                Padding = 10
            };

            var stack = new VerticalStackLayout { Spacing = 6 };
            stack.Add(new Label
            {
                Text = $"{item.CodItem} — {item.Nome} (Total: {item.QuantidadeSolicitada})",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0f172a")
            });

            var linhaQtds = new HorizontalStackLayout { Spacing = 10 };
            linhaQtds.Add(new Label { Text = "Bom Estado:", FontSize = 11, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#16a34a") });
            linhaQtds.Add(entryBom);
            linhaQtds.Add(new Label { Text = "Avariado:", FontSize = 11, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#dc2626") });
            linhaQtds.Add(entryAvariado);

            stack.Add(linhaQtds);
            stack.Add(entryMotivo);
            box.Content = stack;
            ListaClassificacaoStack.Children.Add(box);
        }

        OverlayClassificacao.IsVisible = true;
    }

    private async void OnConfirmarClassificacaoClicked(object sender, EventArgs e)
    {
        if (_devolucaoAtual == null) return;

        var payload = new List<ItemClassificadoDevolucaoInput>();

        foreach (var ui in _itensClassificacaoUi)
        {
            if (!int.TryParse(ui.EntryBom.Text?.Trim(), out int qtdBom) || qtdBom < 0 ||
                !int.TryParse(ui.EntryAvariado.Text?.Trim(), out int qtdAv) || qtdAv < 0 ||
                (qtdBom + qtdAv) != ui.Total)
            {
                await DisplayAlert("Quantidade Inválida", $"A soma de Bom Estado + Avariado de '{ui.Nome}' deve ser igual a {ui.Total}.", "OK");
                return;
            }

            payload.Add(new ItemClassificadoDevolucaoInput
            {
                CodItem = ui.CodItem,
                QuantidadeBomEstado = qtdBom,
                QuantidadeAvariada = qtdAv,
                MotivoAvaria = ui.EntryMotivo.Text
            });
        }

        var (sucesso, msg) = await _pedidoService.ClassificarItensDevolucaoAsync(_devolucaoAtual.Id, payload, _usuarioLogado);
        if (!sucesso)
        {
            await DisplayAlert("Erro", msg, "OK");
            return;
        }

        OverlayClassificacao.IsVisible = false;
        await DisplayAlert("Concluído", msg, "OK");
        await CarregarDevolucoesAsync();
    }

    private void OnFecharOverlaysClicked(object sender, EventArgs e)
    {
        OverlayVolumes.IsVisible = false;
        OverlayBipagemItens.IsVisible = false;
        OverlayClassificacao.IsVisible = false;
    }

    private sealed class ClassificacaoItemUi
    {
        public string CodItem { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int Total { get; set; }
        public Entry EntryBom { get; set; } = null!;
        public Entry EntryAvariado { get; set; } = null!;
        public Entry EntryMotivo { get; set; } = null!;
    }
}