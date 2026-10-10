using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class MainPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private string _statusAtualFiltro = "Aguardando";
    private readonly int _perfilLogado;
    private List<Pedido> _pedidosAnteriores = new();
    private List<Pedido> _pedidosAtivos = new();

    private Pedido _pedidoSendoEstimado;

    public MainPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
        BindingContext = this;

        string matriculaSalva = Preferences.Get("MatriculaLogada", "3");
        if (!int.TryParse(matriculaSalva, out _perfilLogado))
            _perfilLogado = 3;

        AplicarRegrasDeAcessoPorPerfil();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarPedidosAsync();
    }

    private void AplicarRegrasDeAcessoPorPerfil()
    {
        if (_perfilLogado == 1)
        {
            ContainerAbaSeparacao.IsVisible = true;
            ContainerAbaConferencia.IsVisible = false;
            ContainerAbaExpedicao.IsVisible = true;
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
        else if (_perfilLogado == 2)
        {
            ContainerAbaSeparacao.IsVisible = false;
            ContainerAbaConferencia.IsVisible = true;
            ContainerAbaExpedicao.IsVisible = true;
            _statusAtualFiltro = "Aguardando Conferencia";
            AtualizarVisualAbas(AbaConferenciaBtn);
        }
        else
        {
            ContainerAbaSeparacao.IsVisible = true;
            ContainerAbaConferencia.IsVisible = true;
            ContainerAbaExpedicao.IsVisible = true;
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
    }

    private async void OnPerfilClicked(object sender, EventArgs e)
    {
        string nomePerfil = _perfilLogado switch
        {
            1 => "Separador",
            2 => "Conferente",
            3 => "Híbrido",
            4 => "Supervisor",
            _ => "Usuário Padrão"
        };

        string acao;
        string matriculaSalva = Preferences.Get("MatriculaLogada", "");

        if (_perfilLogado == 4 || matriculaSalva == "4" || matriculaSalva.Contains("supervisor"))
        {
            acao = await DisplayActionSheet($"Perfil: {nomePerfil}", "Cancelar", null, "📊 Relatórios de Produtividade", "🚪 Sair da Conta");

            if (acao == "📊 Relatórios de Produtividade")
            {
                var paginaSuper = Handler.MauiContext.Services.GetService<GestaoSupervisorPage>();
                await Navigation.PushAsync(paginaSuper);
                return;
            }
        }
        else
        {
            acao = await DisplayActionSheet($"Perfil: {nomePerfil}", "Cancelar", "🚪 Sair da Conta");
        }

        if (acao == "🚪 Sair da Conta" || acao == "Sair da Conta")
        {
            Preferences.Remove("MatriculaLogada");
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }

    private async void OnSincronizarClicked(object sender, EventArgs e)
    {
        await ExecutarComLoadingAsync(SincronizarBtn, "Buscando...", async () =>
        {
            await CarregarPedidosAsync();
        });
    }

    private async Task CarregarPedidosAsync()
    {
        ExibirSkeleton(true);

        PesquisaExpedicaoBar.Text = string.Empty;

        _pedidosAtivos = await _pedidoService.ObterPorStatusAsync(_statusAtualFiltro);
        PedidosCollectionView.ItemsSource = _pedidosAtivos;

        ExibirSkeleton(false);

        if (ListaPedidosAnteriores.IsVisible)
        {
            await CarregarPedidosAnterioresAsync();
        }
    }

    private void ExibirSkeleton(bool visivel)
    {
        SkeletonLoadingView.IsVisible = visivel;
        PedidosCollectionView.IsVisible = !visivel;
    }

    private async Task CarregarPedidosAnterioresAsync()
    {
        var todosPedidos = await _pedidoService.GetPedidosAsync();

        if (_statusAtualFiltro == "Aguardando")
            _pedidosAnteriores = todosPedidos.Where(p => p.Status != "Aguardando" && p.Status != "Em Separaçao").ToList();
        else if (_statusAtualFiltro == "Aguardando Conferencia")
            _pedidosAnteriores = todosPedidos.Where(p => p.Status == "Aguardando Expediçao" || p.Status == "Expedido" || p.Status == "Finalizado").ToList();
        else if (_statusAtualFiltro == "Aguardando Expediçao")
            _pedidosAnteriores = todosPedidos.Where(p => p.Status == "Expedido" || p.Status == "Finalizado").ToList();
        else
            _pedidosAnteriores = new List<Pedido>();

        BindableLayout.SetItemsSource(ListaPedidosAnteriores, _pedidosAnteriores);
    }

    private async void OnTogglePedidosAnterioresTapped(object sender, TappedEventArgs e)
    {
        ListaPedidosAnteriores.IsVisible = !ListaPedidosAnteriores.IsVisible;
        SetaPedidosAnteriores.Text = ListaPedidosAnteriores.IsVisible ? "▲" : "▼";

        if (ListaPedidosAnteriores.IsVisible)
        {
            await CarregarPedidosAnterioresAsync();
        }
    }

    private void OnPedidoAnteriorItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is Pedido pedidoTapped)
        {
            pedidoTapped.IsExpanded = !pedidoTapped.IsExpanded;
            BindableLayout.SetItemsSource(ListaPedidosAnteriores, null);
            BindableLayout.SetItemsSource(ListaPedidosAnteriores, _pedidosAnteriores);
        }
    }

    private async void OnAbaClicked(object sender, EventArgs e)
    {
        if (sender is Button abaClicada && abaClicada.CommandParameter is string status)
        {
            _statusAtualFiltro = status;
            AtualizarVisualAbas(abaClicada);

            PesquisaExpedicaoBar.IsVisible = status == "Aguardando Expediçao";

            await CarregarPedidosAsync();
        }
    }

    private async void OnPesquisaExpedicaoPesquisar(object sender, EventArgs e)
    {
        string termo = PesquisaExpedicaoBar.Text?.Trim().ToLower() ?? "";
        if (string.IsNullOrEmpty(termo))
        {
            PedidosCollectionView.ItemsSource = _pedidosAtivos;
            return;
        }

        var filtrados = _pedidosAtivos.Where(p =>
            (p.Cliente != null && p.Cliente.ToLower().Contains(termo)) ||
            p.IdPedido.ToString() == termo
        ).ToList();

        if (filtrados.Count > 1)
        {
            bool lote = await DisplayAlert("Expedição em Lote", $"Encontramos {filtrados.Count} pedidos aguardando expedição. Deseja selecionar TODOS para bipar os volumes juntos?", "Sim, Todos", "Não, apenas filtrar");
            if (lote)
            {
                await Navigation.PushAsync(new ExpedicaoItensPage(filtrados, _pedidoService));
                return;
            }
        }

        PedidosCollectionView.ItemsSource = filtrados;
    }

    private void OnPesquisaExpedicaoTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            PedidosCollectionView.ItemsSource = _pedidosAtivos;
        }
    }

    private void AtualizarVisualAbas(Button abaAtiva)
    {
        var mapaAbas = new (Button Botao, Border Pill)[]
        {
            (AbaSeparacaoBtn, PillSeparacao),
            (AbaConferenciaBtn, PillConferencia),
            (AbaExpedicaoBtn, PillExpedicao)
        };

        foreach (var item in mapaAbas)
        {
            if (abaAtiva != null && item.Botao == abaAtiva)
            {
                item.Botao.TextColor = Colors.White;
                item.Pill.BackgroundColor = Color.FromArgb("#2563eb");
            }
            else
            {
                item.Botao.TextColor = Color.FromArgb("#64748b");
                item.Pill.BackgroundColor = Colors.Transparent;
            }
        }
    }

    private void OnEnviarEstimativaSupervisorClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Pedido pedido)
        {
            _pedidoSendoEstimado = pedido;

            LabelPedidoEstimativa.Text = $"Pedido #{pedido.IdPedido:D4} - {pedido.Cliente}";
            EstimativaDatePicker.Date = DateTime.Today;
            EstimativaTimePicker.Time = DateTime.Now.TimeOfDay;

            OverlayEstimativa.IsVisible = true;
        }
    }

    private void OnCancelarEstimativaClicked(object sender, EventArgs e)
    {
        OverlayEstimativa.IsVisible = false;
        _pedidoSendoEstimado = null;
    }

    private async void OnConfirmarEstimativaClicked(object sender, EventArgs e)
    {
        if (_pedidoSendoEstimado != null)
        {
            DateTime data = Convert.ToDateTime(EstimativaDatePicker.Date);
            string dataFormatada = $"{data.Day:00}/{data.Month:00}/{data.Year}";

            string horaBruta = EstimativaTimePicker.Time.ToString();
            string horaFormatada = horaBruta.Length >= 5 ? horaBruta.Substring(0, 5) : horaBruta;

            string dataHoraFinal = $"{dataFormatada} {horaFormatada}";

            OverlayEstimativa.IsVisible = false;

            var (sucesso, mensagem) = await _pedidoService.EnviarEstimativaAsync(_pedidoSendoEstimado.IdPedido, dataHoraFinal);

            if (sucesso)
            {
                await DisplayAlert("Sucesso", "Data e hora estimada enviadas com sucesso ao comercial!", "OK");
                await CarregarPedidosAsync();
            }
            else
            {
                await DisplayAlert("Erro", $"Falha ao enviar estimativa: {mensagem}", "OK");
            }

            _pedidoSendoEstimado = null;
        }
    }

    private async void OnIniciarAcaoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Pedido pedidoSelecionado)
        {
            await ExecutarComLoadingAsync(button, "Aguarde...", async () =>
            {
                if (pedidoSelecionado.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.IdPedido, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Separaçao";
                        await Navigation.PushAsync(new SeparacaoItensPage(pedidoSelecionado, _pedidoService));
                    }
                    else await DisplayAlert("Erro", mensagem, "OK");
                }
                else if (pedidoSelecionado.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.IdPedido, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Conferencia";
                        await Navigation.PushAsync(new ConferenciaItensPage(pedidoSelecionado, _pedidoService));
                    }
                    else await DisplayAlert("Erro", mensagem, "OK");
                }
                else if (pedidoSelecionado.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(new ExpedicaoItensPage(pedidoSelecionado, _pedidoService));
                }
            });
        }
    }

    private async Task ExecutarComLoadingAsync(Button botao, string textoLoading, Func<Task> acao)
    {
        if (!botao.IsEnabled) return;

        string textoOriginal = botao.Text;
        botao.Text = textoLoading;
        botao.IsEnabled = false;
        botao.Opacity = 0.7;

        try { await acao(); }
        finally
        {
            botao.Text = textoOriginal;
            botao.IsEnabled = true;
            botao.Opacity = 1.0;
        }
    }
}