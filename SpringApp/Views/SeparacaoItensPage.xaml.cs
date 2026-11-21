using System.Text.RegularExpressions;
using SpringApp.Helpers;
using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class SeparacaoItensPage : ContentPage
{
    private Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private bool _bloqueioFoco = false;
    private readonly int _pedidoId;
    private int _errosEvitados = 0;
    private List<ItemPedido> _itensComuns = new();
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public SeparacaoItensPage(Pedido pedido, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
        _pedido = pedido;
        _pedidoId = pedido?.IdPedido ?? 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_pedidoId == 0) return;

        await _pedidoService.BloquearPedidoAsync(_pedidoId, _usuarioLogado);
        await CarregarPedidoDaApiAsync();
        GarantirFocoLeitor();
    }

    private void OnLeitorLoaded(object? sender, EventArgs e)
    {
        GarantirFocoLeitor();
    }

    private void GarantirFocoLeitor()
    {
        if (_bloqueioFoco || _isFinalizando) return;

        Dispatcher.Dispatch(() =>
        {
            if (!_bloqueioFoco && !_isFinalizando)
                LeitorBluetoothEntry.Focus();
        });

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
        {
            if (!_bloqueioFoco && !_isFinalizando)
                LeitorBluetoothEntry.Focus();
        });
    }

    private void OnLeitorUnfocused(object sender, FocusEventArgs e)
    {
        if (!_bloqueioFoco && !_isFinalizando)
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
            {
                if (!_bloqueioFoco && !_isFinalizando)
                    LeitorBluetoothEntry.Focus();
            });
        }
    }

    private void OnTelaTapped(object sender, TappedEventArgs e)
    {
        GarantirFocoLeitor();
    }

    protected override bool OnBackButtonPressed() => true;

    private static int CompararEnderecoNatural(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (string.IsNullOrWhiteSpace(x)) return 1;
        if (string.IsNullOrWhiteSpace(y)) return -1;

        var tokensX = Regex.Matches(x.Trim().ToUpperInvariant(), @"(\d+|\D+)");
        var tokensY = Regex.Matches(y.Trim().ToUpperInvariant(), @"(\d+|\D+)");

        int limite = Math.Min(tokensX.Count, tokensY.Count);
        for (int i = 0; i < limite; i++)
        {
            string tx = tokensX[i].Value;
            string ty = tokensY[i].Value;

            bool numX = int.TryParse(tx, out int nx);
            bool numY = int.TryParse(ty, out int ny);

            int cmp = (numX && numY)
                ? nx.CompareTo(ny)
                : string.Compare(tx, ty, StringComparison.OrdinalIgnoreCase);

            if (cmp != 0) return cmp;
        }

        return tokensX.Count.CompareTo(tokensY.Count);
    }

    private async Task CarregarPedidoDaApiAsync()
    {
        try
        {
            var p = await _pedidoService.GetPedidoPorIdAsync(_pedidoId);
            if (p != null) _pedido = p;
        }
        catch { }

        NumeroPedidoLabel.Text = $"#{_pedido.IdPedido:D4}";
        ClienteLabel.Text = (_pedido.Cliente ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();

        CardNotificacaoEstimativaSep.IsVisible = _pedido.EstimativaPendenteSeparacao;

        // Filtra apenas os itens comuns (TipoRota == 0) e aplica a Rota Inteligente:
        // 1º Rua (Rx-y-z) em ordem natural | 2º Pulmao / Caixa Fechada (Ax-px) em ordem natural
        _itensComuns = (_pedido.Itens ?? new List<ItemPedido>())
            .Where(i => i.TipoRota == 0)
            .OrderBy(i => i.IsCaixaFechada ? 1 : 0)
            .ThenBy(i => i.IsCaixaFechada ? i.LocalPulmao : i.LocalPicking, Comparer<string?>.Create(CompararEnderecoNatural))
            .ThenBy(i => i.CodItem, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ItensCollectionView.ItemsSource = _itensComuns;
        AtualizarContadores();
        GarantirFocoLeitor();
    }

    private void OnAbrirModalEstimativaSepClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        int faltam = _itensComuns.Sum(i => i.SeparacaoPendente);
        LabelResumoItensSep.Text = $"Faltam separar {faltam} volumes/unidades neste pedido";
        EstimativaSepDatePicker.Date = DateTime.Today;
        EstimativaSepTimePicker.Time = DateTime.Now.TimeOfDay;
        OverlayEstimativaSep.IsVisible = true;
    }

    private void OnCancelarModalEstimativaSepClicked(object sender, EventArgs e)
    {
        OverlayEstimativaSep.IsVisible = false;
        _bloqueioFoco = false;
        GarantirFocoLeitor();
    }

    private async void OnConfirmarModalEstimativaSepClicked(object sender, EventArgs e)
    {
        DateTime data = Convert.ToDateTime(EstimativaSepDatePicker.Date);
        string dataFormatada = $"{data.Day:00}/{data.Month:00}/{data.Year}";

        string horaBruta = EstimativaSepTimePicker.Time.ToString();
        string horaFormatada = horaBruta.Length >= 5 ? horaBruta.Substring(0, 5) : horaBruta;
        string dataHoraFinal = $"{dataFormatada} {horaFormatada}";

        OverlayEstimativaSep.IsVisible = false;

        var (sucesso, mensagem) = await _pedidoService.EnviarEstimativaAsync(
            _pedido.IdPedido,
            dataHoraFinal,
            "Separacao",
            _usuarioLogado);

        if (sucesso)
        {
            _pedido.EstimativaPendenteSeparacao = false;
            _pedido.EstimativaPendenteConferencia = true;
            _pedido.EstimativaSeparacao = dataHoraFinal;
            _pedido.EstimativaSeparacaoUser = _usuarioLogado;
            CardNotificacaoEstimativaSep.IsVisible = false;

            await DisplayAlert("Sucesso", "Previsão da separação registrada!", "OK");
        }
        else
        {
            await DisplayAlert("Erro", $"Falha ao enviar estimativa: {mensagem}", "OK");
        }

        _bloqueioFoco = false;
        GarantirFocoLeitor();
    }

    private void OnLeitorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue)) return;

        if (e.NewTextValue.Contains('\r') || e.NewTextValue.Contains('\n'))
        {
            string codigoLimpo = e.NewTextValue.Replace("\r", "").Replace("\n", "").Trim();
            LeitorBluetoothEntry.Text = string.Empty;
            if (!string.IsNullOrWhiteSpace(codigoLimpo))
            {
                _ = ValidarCodigoLidoAsync(codigoLimpo);
            }
        }
    }

    private async void OnCodigoScaneadoCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.Text))
        {
            string codigo = entry.Text.Replace("\r", "").Replace("\n", "").Trim();
            entry.Text = string.Empty;
            await ValidarCodigoLidoAsync(codigo);
        }
    }

    private ItemPedido? BuscarItemPorCodigoBipado(string codigo)
    {
        string codLimpo = codigo.Trim();

        // Prioriza item pendente que exija exatamente o codigo bipado (ex: CX-001 para caixa fechada ou 001 para avulso)
        var pendente = _itensComuns.FirstOrDefault(i =>
            i.SeparacaoPendente > 0 &&
            i.CodigoBipagemEsperado.Equals(codLimpo, StringComparison.OrdinalIgnoreCase));

        if (pendente != null) return pendente;

        // Caso ja esteja concluido, verifica se o codigo pertence a algum item da lista para nao acusar erro falso
        return _itensComuns.FirstOrDefault(i =>
            i.CodigoBipagemEsperado.Equals(codLimpo, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ValidarCodigoLidoAsync(string codigo)
    {
        var item = BuscarItemPorCodigoBipado(codigo);

        if (item != null)
        {
            ProcessarBipagemItem(item);
        }
        else
        {
            _errosEvitados++;
            _bloqueioFoco = true;
            try
            {
                bool bipouAvulsoEmCaixaFechada = _itensComuns.Any(i =>
                    i.IsCaixaFechada &&
                    i.SeparacaoPendente > 0 &&
                    i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

                string msgAlerta = bipouAvulsoEmCaixaFechada
                    ? "Este item exige a bipagem do código de CAIXA FECHADA no Pulmão!"
                    : "Código inválido ou pertencente à Área Técnica.";

                await DisplayAlert("Atenção", msgAlerta, "OK");
            }
            finally
            {
                _bloqueioFoco = false;
            }
        }

        GarantirFocoLeitor();
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string codigo = await DisplayPromptAsync("Manual", "Informe o SKU ou Código da Caixa Fechada:", "Confirmar", "Cancelar");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var item = BuscarItemPorCodigoBipado(codigo);
                if (item != null)
                {
                    ProcessarBipagemItem(item);
                }
                else
                {
                    _errosEvitados++;
                    bool bipouAvulsoEmCaixaFechada = _itensComuns.Any(i =>
                        i.IsCaixaFechada &&
                        i.SeparacaoPendente > 0 &&
                        i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

                    string msgErro = bipouAvulsoEmCaixaFechada
                        ? "Informe o código da CAIXA FECHADA (não o SKU unitário)."
                        : "Produto não encontrado nesta lista.";

                    await DisplayAlert("Erro", msgErro, "OK");
                }
            }
        }
        finally
        {
            _bloqueioFoco = false;
            GarantirFocoLeitor();
        }
    }

    private void ProcessarBipagemItem(ItemPedido item)
    {
        if (item.BipagensSeparacao < item.Quantidade)
        {
            item.BipagensSeparacao++;
            AtualizarContadores();
        }
    }

    private void AtualizarContadores()
    {
        int total = _itensComuns.Sum(i => i.Quantidade);
        int sep = _itensComuns.Sum(i => i.BipagensSeparacao);

        TotalRestanteLabel.Text = sep.ToString();
        TotalGeralLabel.Text = total.ToString();

        if (total - sep <= 0 && _itensComuns.Any())
        {
            _ = FinalizarSeparacaoAutomaticaAsync();
        }
        else
        {
            GarantirFocoLeitor();
        }
    }

    private async void OnAguardarClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string? motivo = await JustificativaHelper.SolicitarJustificativaAsync(this, "Colocar em Espera");
            if (string.IsNullOrWhiteSpace(motivo)) return;

            bool confirm = await DisplayAlert(
                "Colocar em Espera",
                $"Motivo: {motivo}\nDeseja pausar esta separação e manter os itens já bipados salvos?",
                "Sim, Aguardar",
                "Não");

            if (confirm)
            {
                var (sucesso, msg) = await _pedidoService.PausarPedidoAsync(_pedido, "Separação", _usuarioLogado, _errosEvitados, motivo);
                if (sucesso)
                {
                    await Navigation.PopAsync();
                    return;
                }
                await DisplayAlert("Erro", msg, "OK");
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }
        GarantirFocoLeitor();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string? motivo = await JustificativaHelper.SolicitarJustificativaAsync(this, "Cancelar Progresso");
            if (string.IsNullOrWhiteSpace(motivo)) return;

            bool confirm = await DisplayAlert(
                "Atenção",
                $"Motivo: {motivo}\nDeseja sair e cancelar o progresso atual?",
                "Sim",
                "Não");

            if (confirm)
            {
                await _pedidoService.ReverterEtapaAsync(_pedido.IdPedido, motivo, _usuarioLogado);
                await Navigation.PopAsync();
                return;
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }
        GarantirFocoLeitor();
    }

    private async Task FinalizarSeparacaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        var (sucesso, msg) = await _pedidoService.ConcluirSeparacaoRotaAsync(_pedido, 0, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            var pedidoAtualizado = await _pedidoService.GetPedidoPorIdAsync(_pedidoId);
            bool liberadoConferencia = pedidoAtualizado != null &&
                                       pedidoAtualizado.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase);

            string textoSucesso = liberadoConferencia
                ? "Separação concluída! Todos os itens foram bipados e o pedido foi liberado para Conferência."
                : "Itens comuns separados! O pedido aguarda a Área Técnica bipar os itens técnicos para liberar a Conferência.";

            await DisplayAlert("Sucesso", textoSucesso, "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", msg, "OK");
            GarantirFocoLeitor();
        }
    }
}