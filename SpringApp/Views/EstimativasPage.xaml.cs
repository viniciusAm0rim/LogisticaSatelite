using System.Globalization;
using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class EstimativasPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private readonly int _perfilLogado;
    private Pedido _pedidoSendoEstimado;
    private DateTime? _limiteMinimoConferencia;
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public EstimativasPage(IPedidoService pedidoService, int perfilLogado)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
        _perfilLogado = perfilLogado;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarEstimativasPendentesAsync();
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CarregarEstimativasPendentesAsync();
        EstimativasRefreshView.IsRefreshing = false;
    }

    // Carrega os pedidos em fila com solicitacao de estimativa compativeis com o perfil
    private async Task CarregarEstimativasPendentesAsync()
    {
        try
        {
            var todosPedidos = await _pedidoService.GetPedidosAsync();

            var pendentes = todosPedidos.Where(p =>
            {
                bool emExecucao = p.Status.Contains("Em Separa", StringComparison.OrdinalIgnoreCase) ||
                                  p.Status.Contains("Em Confer", StringComparison.OrdinalIgnoreCase);

                if (emExecucao) return false;

                if (_perfilLogado == 1)
                    return p.EstimativaPendenteSeparacao;

                if (_perfilLogado == 2)
                    return p.EstimativaPendenteConferencia;

                return p.EstimativaPendenteSeparacao || p.EstimativaPendenteConferencia;
            }).ToList();

            EstimativasCollectionView.ItemsSource = pendentes;
        }
        catch (Exception)
        {
            await DisplayAlert("Erro", "Não foi possível carregar as estimativas pendentes.", "OK");
        }
    }

    // Abre a ficha tecnica do pedido e trava a data/hora minima quando for conferencia
    private async void OnAbrirFichaEstimativaClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Pedido pedidoSelecionado)
        {
            var pedidoCompleto = await _pedidoService.GetPedidoPorIdAsync(pedidoSelecionado.IdPedido) ?? pedidoSelecionado;
            _pedidoSendoEstimado = pedidoCompleto;
            _limiteMinimoConferencia = null;

            LabelPedidoEstimativa.Text = $"Pedido #{pedidoCompleto.IdPedido:D4}";

            if (pedidoCompleto.EstimativaPendenteConferencia && !string.IsNullOrWhiteSpace(pedidoCompleto.EstimativaSeparacao))
            {
                string autorSep = string.IsNullOrWhiteSpace(pedidoCompleto.EstimativaSeparacaoUser) ? "Separador" : pedidoCompleto.EstimativaSeparacaoUser;
                LabelInfoSeparadorEstimativa.Text = $"{autorSep}: {pedidoCompleto.EstimativaSeparacao}";
                BoxInfoSeparadorEstimativa.IsVisible = true;

                if (DateTime.TryParseExact(pedidoCompleto.EstimativaSeparacao.Trim(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtSep))
                {
                    _limiteMinimoConferencia = dtSep;
                    DateTime referenciaInicial = DateTime.Now > dtSep ? DateTime.Now : dtSep;

                    EstimativaDatePicker.MinimumDate = dtSep.Date;
                    EstimativaDatePicker.Date = referenciaInicial.Date;
                    EstimativaTimePicker.Time = new TimeSpan(referenciaInicial.Hour, referenciaInicial.Minute, 0);
                }
                else
                {
                    EstimativaDatePicker.MinimumDate = DateTime.Today;
                    EstimativaDatePicker.Date = DateTime.Today;
                    EstimativaTimePicker.Time = DateTime.Now.TimeOfDay;
                }
            }
            else
            {
                BoxInfoSeparadorEstimativa.IsVisible = false;
                EstimativaDatePicker.MinimumDate = DateTime.Today;
                EstimativaDatePicker.Date = DateTime.Today;
                EstimativaTimePicker.Time = DateTime.Now.TimeOfDay;
            }

            ItensFichaEstimativaView.ItemsSource = pedidoCompleto.Itens;
            OverlayEstimativa.IsVisible = true;
        }
    }

    private void OnCancelarEstimativaClicked(object sender, EventArgs e)
    {
        OverlayEstimativa.IsVisible = false;
        _pedidoSendoEstimado = null;
        _limiteMinimoConferencia = null;
    }

    // Valida o limite minimo e envia a estimativa com assinatura do operador
    private async void OnConfirmarEstimativaClicked(object sender, EventArgs e)
    {
        if (_pedidoSendoEstimado == null) return;

        DateTime dataSelecionada = Convert.ToDateTime(EstimativaDatePicker.Date).Date;
        TimeSpan horaSelecionada = EstimativaTimePicker.Time ?? TimeSpan.Zero;
        DateTime dataHoraEscolhida = dataSelecionada.Add(new TimeSpan(horaSelecionada.Hours, horaSelecionada.Minutes, 0));

        // Bloqueia se o conferente tentar informar data/hora anterior ao separador
        if (_pedidoSendoEstimado.EstimativaPendenteConferencia &&
            _limiteMinimoConferencia.HasValue &&
            dataHoraEscolhida < _limiteMinimoConferencia.Value)
        {
            await DisplayAlert(
                "Horário Inválido",
                $"A estimativa deve ser a partir de {_limiteMinimoConferencia.Value:dd/MM/yyyy HH:mm}.",
                "OK");
            return;
        }

        string dataHoraFinal = $"{dataHoraEscolhida:dd/MM/yyyy HH:mm}";
        string etapaAtual = _pedidoSendoEstimado.EstimativaPendenteSeparacao ? "Separacao" : "Conferencia";

        OverlayEstimativa.IsVisible = false;

        var (sucesso, mensagem) = await _pedidoService.EnviarEstimativaAsync(
            _pedidoSendoEstimado.IdPedido,
            dataHoraFinal,
            etapaAtual,
            _usuarioLogado);

        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Estimativa registrada com sucesso!", "OK");
            await CarregarEstimativasPendentesAsync();
        }
        else
        {
            await DisplayAlert("Erro", $"Falha ao enviar estimativa: {mensagem}", "OK");
        }

        _pedidoSendoEstimado = null;
        _limiteMinimoConferencia = null;
    }
}