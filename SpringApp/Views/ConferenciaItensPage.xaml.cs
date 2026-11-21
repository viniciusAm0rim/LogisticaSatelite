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

    private void OnCancelarModalEstimativaConfClicked(object sender, EventArgs e)
    {
        OverlayEstimativaConf.IsVisible = false;
        _limiteMinimoConferencia = null;
        _bloqueioFoco = false;
        GarantirFocoLeitor();
    }

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

    private ItemPedido? BuscarItemConferencia(string codigo)
    {
        string codLimpo = codigo.Trim();

        // Exige estritamente o codigo esperado (CodCaixaFechada para caixa fechada | CodItem para unitario), igual na separacao
        var pendente = _pedido.Itens.FirstOrDefault(i =>
            i.ConferenciaPendente > 0 &&
            i.CodigoBipagemEsperado.Equals(codLimpo, StringComparison.OrdinalIgnoreCase));

        if (pendente != null) return pendente;

        // Fallback para itens ja concluidos para nao acusar erro falso em leitura duplicada
        return _pedido.Itens.FirstOrDefault(i =>
            i.CodigoBipagemEsperado.Equals(codLimpo, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ValidarCodigoLidoAsync(string codigo)
    {
        var item = BuscarItemConferencia(codigo);
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
                bool bipouAvulsoEmCaixaFechada = _pedido.Itens.Any(i =>
                    i.IsCaixaFechada &&
                    i.ConferenciaPendente > 0 &&
                    i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

                string msgAlerta = bipouAvulsoEmCaixaFechada
                    ? "Este item exige a bipagem do código de CAIXA FECHADA!"
                    : "Código de produto ou caixa fechada inválido.";

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
                var item = BuscarItemConferencia(codigo);
                if (item != null)
                {
                    ProcessarBipagemItem(item);
                }
                else
                {
                    _errosEvitados++;
                    bool bipouAvulsoEmCaixaFechada = _pedido.Itens.Any(i =>
                        i.IsCaixaFechada &&
                        i.ConferenciaPendente > 0 &&
                        i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

                    string msgErro = bipouAvulsoEmCaixaFechada
                        ? "Informe o código da CAIXA FECHADA (não o SKU unitário)."
                        : "Produto não encontrado.";

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
            AbrirModalVolumesEObservacao();
        }
        else
        {
            GarantirFocoLeitor();
        }
    }

    private void AbrirModalVolumesEObservacao()
    {
        _bloqueioFoco = true;
        QtdVolumesEntry.Text = "1";
        ObservacaoConferenciaEditor.Text = string.Empty;
        OverlayVolumesObservacao.IsVisible = true;
    }

    private void OnDiminuirVolumesClicked(object sender, EventArgs e)
    {
        int atual = int.TryParse(QtdVolumesEntry.Text, out int v) ? v : 1;
        if (atual > 1)
        {
            QtdVolumesEntry.Text = (atual - 1).ToString();
        }
    }

    private void OnAumentarVolumesClicked(object sender, EventArgs e)
    {
        int atual = int.TryParse(QtdVolumesEntry.Text, out int v) ? v : 1;
        if (atual < 99)
        {
            QtdVolumesEntry.Text = (atual + 1).ToString();
        }
    }

    private void OnCancelarModalVolumesObservacaoClicked(object sender, EventArgs e)
    {
        OverlayVolumesObservacao.IsVisible = false;
        _bloqueioFoco = false;
        GarantirFocoLeitor();
    }

    private async void OnConfirmarVolumesObservacaoClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(QtdVolumesEntry.Text?.Trim(), out int qtdVolumes) || qtdVolumes <= 0)
        {
            await DisplayAlert("Atenção", "Informe uma quantidade válida de volumes (mínimo 1).", "OK");
            return;
        }

        string? observacao = ObservacaoConferenciaEditor.Text?.Trim();

        OverlayVolumesObservacao.IsVisible = false;
        await FinalizarConferenciaNoBancoAsync(qtdVolumes, 0, observacao);
    }

    private async Task FinalizarConferenciaNoBancoAsync(int qtdVolumes, double peso, string? observacao)
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        var (sucesso, msg) = await _pedidoService.ConcluirConferenciaAsync(_pedido.IdPedido, qtdVolumes, peso, observacao, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            _bloqueioFoco = false;
            await DisplayAlert("Sucesso", "Conferência concluída com sucesso!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            _bloqueioFoco = false;
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