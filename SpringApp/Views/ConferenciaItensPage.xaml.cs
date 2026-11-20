using System.Globalization;
using SpringApp.Helpers;
using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class ConferenciaItensPage : ContentPage
{
    private Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private bool _bloqueioFoco = false;
    private int _errosEvitados = 0;
    private DateTime? _limiteMinimoConferencia;
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public ConferenciaItensPage(Pedido pedido, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedido = pedido;
        _pedidoService = pedidoService;
        CarregarDados();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _pedidoService.BloquearPedidoAsync(_pedido.IdPedido, _usuarioLogado);

        try
        {
            var atualizado = await _pedidoService.GetPedidoPorIdAsync(_pedido.IdPedido);
            if (atualizado != null)
            {
                _pedido = atualizado;
                CarregarDados();
            }
        }
        catch { }

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

    // Preenche os dados do pedido e exibe o card enxuto de estimativa caso esteja pendente
    private void CarregarDados()
    {
        NumeroPedidoLabel.Text = $"#{_pedido.IdPedido:D4}";
        ClienteLabel.Text = (_pedido.Cliente ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();

        CardNotificacaoEstimativaConf.IsVisible = _pedido.EstimativaPendenteConferencia;
        if (_pedido.EstimativaPendenteConferencia && !string.IsNullOrWhiteSpace(_pedido.EstimativaSeparacao))
        {
            string autorSep = string.IsNullOrWhiteSpace(_pedido.EstimativaSeparacaoUser) ? "Separador" : _pedido.EstimativaSeparacaoUser;
            LabelAvisoSeparacaoNaConf.Text = $"{autorSep}: {_pedido.EstimativaSeparacao}";
        }
        else
        {
            LabelAvisoSeparacaoNaConf.Text = "Informar previsão";
        }

        ItensCollectionView.ItemsSource = _pedido.Itens;
        AtualizarContadoresItens();
        GarantirFocoLeitor();
    }

    // Abre o formulario de estimativa da conferencia travado a partir da data/hora do separador
    private void OnAbrirModalEstimativaConfClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        _limiteMinimoConferencia = null;

        if (!string.IsNullOrWhiteSpace(_pedido.EstimativaSeparacao))
        {
            string autorSep = string.IsNullOrWhiteSpace(_pedido.EstimativaSeparacaoUser) ? "Separador" : _pedido.EstimativaSeparacaoUser;
            LabelModalSeparadorConf.Text = $"{autorSep}: {_pedido.EstimativaSeparacao}";
            LabelModalSeparadorConf.IsVisible = true;

            if (DateTime.TryParseExact(_pedido.EstimativaSeparacao.Trim(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtSep))
            {
                _limiteMinimoConferencia = dtSep;
                DateTime referenciaInicial = DateTime.Now > dtSep ? DateTime.Now : dtSep;

                EstimativaConfDatePicker.MinimumDate = dtSep.Date;
                EstimativaConfDatePicker.Date = referenciaInicial.Date;
                EstimativaConfTimePicker.Time = new TimeSpan(referenciaInicial.Hour, referenciaInicial.Minute, 0);
            }
            else
            {
                EstimativaConfDatePicker.MinimumDate = DateTime.Today;
                EstimativaConfDatePicker.Date = DateTime.Today;
                EstimativaConfTimePicker.Time = DateTime.Now.TimeOfDay;
            }
        }
        else
        {
            LabelModalSeparadorConf.IsVisible = false;
            EstimativaConfDatePicker.MinimumDate = DateTime.Today;
            EstimativaConfDatePicker.Date = DateTime.Today;
            EstimativaConfTimePicker.Time = DateTime.Now.TimeOfDay;
        }

        OverlayEstimativaConf.IsVisible = true;
    }

    // Fecha o formulario de estimativa e devolve o foco ao leitor
    private void OnCancelarModalEstimativaConfClicked(object sender, EventArgs e)
    {
        OverlayEstimativaConf.IsVisible = false;
        _limiteMinimoConferencia = null;
        _bloqueioFoco = false;
        GarantirFocoLeitor();
    }

    // Valida o horario minimo e envia a previsao final com assinatura do conferente
    private async void OnConfirmarModalEstimativaConfClicked(object sender, EventArgs e)
    {
        DateTime dataSelecionada = Convert.ToDateTime(EstimativaConfDatePicker.Date).Date;
        TimeSpan horaSelecionada = EstimativaConfTimePicker.Time ?? TimeSpan.Zero;
        DateTime dataHoraEscolhida = dataSelecionada.Add(new TimeSpan(horaSelecionada.Hours, horaSelecionada.Minutes, 0));

        if (_limiteMinimoConferencia.HasValue && dataHoraEscolhida < _limiteMinimoConferencia.Value)
        {
            await DisplayAlert(
                "Horário Inválido",
                $"A estimativa deve ser a partir de {_limiteMinimoConferencia.Value:dd/MM/yyyy HH:mm}.",
                "OK");
            return;
        }

        string dataHoraFinal = $"{dataHoraEscolhida:dd/MM/yyyy HH:mm}";

        OverlayEstimativaConf.IsVisible = false;

        var (sucesso, mensagem) = await _pedidoService.EnviarEstimativaAsync(
            _pedido.IdPedido,
            dataHoraFinal,
            "Conferencia",
            _usuarioLogado);

        if (sucesso)
        {
            _pedido.EstimativaPendenteConferencia = false;
            _pedido.EstimativaSolicitada = false;
            _pedido.EstimativaConferencia = dataHoraFinal;
            _pedido.EstimativaConferenciaUser = _usuarioLogado;
            CardNotificacaoEstimativaConf.IsVisible = false;

            await DisplayAlert("Sucesso", "Estimativa enviada ao comercial!", "OK");
        }
        else
        {
            await DisplayAlert("Erro", $"Falha ao enviar estimativa: {mensagem}", "OK");
        }

        _limiteMinimoConferencia = null;
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
                await DisplayAlert("Atenção", "Código de produto inválido.", "OK");
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
            string codigo = await DisplayPromptAsync("Manual", "Informe o SKU do produto:", "Confirmar", "Cancelar");

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
        if (item.BipagensConferencia < item.Quantidade)
        {
            item.BipagensConferencia++;
            AtualizarContadoresItens();
        }
    }

    private void AtualizarContadoresItens()
    {
        int totalItensGeral = _pedido.Itens.Sum(i => i.Quantidade);
        int totalConferidosGeral = _pedido.Itens.Sum(i => i.BipagensConferencia);
        int totalPendente = _pedido.Itens.Sum(i => i.ConferenciaPendente);

        TotalRestanteLabel.Text = totalConferidosGeral.ToString();
        TotalGeralLabel.Text = totalItensGeral.ToString();

        if (totalPendente <= 0 && _pedido.Itens.Any())
        {
            _ = IniciarEtapaVolumesAsync();
        }
        else
        {
            GarantirFocoLeitor();
        }
    }

    private async Task IniciarEtapaVolumesAsync()
    {
        int volumes = 0;
        bool volumeValido = false;

        _bloqueioFoco = true;
        try
        {
            while (!volumeValido)
            {
                string resposta = await DisplayPromptAsync("Produtos Conferidos!", "Quantos volumes (caixas) este pedido gerou?", "Confirmar", "Cancelar", keyboard: Keyboard.Numeric);

                if (int.TryParse(resposta, out volumes) && volumes > 0)
                {
                    volumeValido = true;
                }
                else
                {
                    await DisplayAlert("Atenção", "É obrigatório informar um número válido de volumes para prosseguir.", "OK");
                }
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }

        await FinalizarConferenciaNoBancoAsync(volumes);
    }

    private async Task FinalizarConferenciaNoBancoAsync(int qtdVolumes)
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        var (sucesso, msg) = await _pedidoService.ConcluirConferenciaAsync(_pedido.IdPedido, qtdVolumes, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Conferência concluída com sucesso!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", msg, "OK");
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
                $"Motivo: {motivo}\nDeseja pausar esta conferência e manter os itens já bipados salvos?",
                "Sim, Aguardar",
                "Não");

            if (confirm)
            {
                var (sucesso, msg) = await _pedidoService.PausarPedidoAsync(_pedido, "Conferência", _usuarioLogado, _errosEvitados, motivo);
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
}