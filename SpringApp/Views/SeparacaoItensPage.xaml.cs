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

        ItensCollectionView.ItemsSource = _pedido.Itens;
        AtualizarContadores();
        GarantirFocoLeitor();
    }

    private void OnAbrirModalEstimativaSepClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        int faltam = _pedido.Itens?.Sum(i => i.SeparacaoPendente) ?? 0;
        LabelResumoItensSep.Text = $"Faltam separar {faltam} unidades neste pedido";
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

    private async Task ValidarCodigoLidoAsync(string codigo)
    {
        var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo, StringComparison.OrdinalIgnoreCase));

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
                await DisplayAlert("Atenção", "Código inválido.", "OK");
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
            string codigo = await DisplayPromptAsync("Manual", "Informe o SKU:", "Confirmar", "Cancelar");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    ProcessarBipagemItem(item);
                }
                else
                {
                    _errosEvitados++;
                    await DisplayAlert("Erro", "Produto não encontrado.", "OK");
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
        if (_pedido?.Itens == null) return;
        int total = _pedido.Itens.Sum(i => i.Quantidade);
        int sep = _pedido.Itens.Sum(i => i.BipagensSeparacao);

        TotalRestanteLabel.Text = sep.ToString();
        TotalGeralLabel.Text = total.ToString();

        if (total - sep <= 0 && _pedido.Itens.Any())
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

        var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(_pedido.IdPedido, _pedido.Status, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Separação concluída!", "OK");
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