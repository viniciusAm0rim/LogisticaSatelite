using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class ExpedicaoItensPage : ContentPage
{
    private readonly Pedido _pedido;
    private readonly PedidoService _pedidoService;
    private bool _isFinalizando = false;
    private int _volumesExpedidos = 0;

    public ExpedicaoItensPage(Pedido pedido)
    {
        InitializeComponent();
        _pedido = pedido;
        _pedidoService = new PedidoService();

        CarregarDados();
    }

    private void CarregarDados()
    {
        NumeroPedidoLabel.Text = $"Pedido #{_pedido.Id} - {_pedido.Numero}";
        ClienteLabel.Text = $"Cliente: {_pedido.Destinatario}";
        SubtituloResponsavelLabel.Text = $"Conf: {_pedido.ConfUser ?? "Não informado"}";
        AtualizarContadores();
    }

    private void OnBiparClicked(object sender, EventArgs e) => ExecutarExpedicaoPorCodigo(CodigoBarraEntry.Text?.Trim());
    private void OnBiparCompleted(object sender, EventArgs e) => ExecutarExpedicaoPorCodigo(CodigoBarraEntry.Text?.Trim());
    private void OnExpedirVolumeDiretoClicked(object sender, EventArgs e) => ProcessarBaixaVolume();

    private void ExecutarExpedicaoPorCodigo(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return;

        if (_pedido.Numero.Equals(codigo, StringComparison.OrdinalIgnoreCase) || _pedido.Id.ToString() == codigo)
        {
            ProcessarBaixaVolume();
        }
        else
        {
            DisplayAlert("Atenção", "Código de barras não corresponde a este pedido!", "OK");
        }

        CodigoBarraEntry.Text = string.Empty;
        CodigoBarraEntry.Focus();
    }

    private void ProcessarBaixaVolume()
    {
        int volumesTotais = _pedido.Volumes > 0 ? _pedido.Volumes : 1;

        if (_volumesExpedidos < volumesTotais)
        {
            _volumesExpedidos++;
            AtualizarContadores();
        }
        else
        {
            DisplayAlert("Aviso", "Todos os volumes deste pedido já foram expedidos!", "OK");
        }
    }

    private void AtualizarContadores()
    {
        int volumesTotais = _pedido.Volumes > 0 ? _pedido.Volumes : 1;
        int volumesPendentes = Math.Max(0, volumesTotais - _volumesExpedidos);

        VolumesRestantesLabel.Text = $"{_volumesExpedidos} de {volumesTotais}";

        _pedido.StatusProgressoVolume = $"{_volumesExpedidos} de {volumesTotais} volumes";

        PedidoCollectionView.ItemsSource = null;
        PedidoCollectionView.ItemsSource = new List<Pedido> { _pedido };

        if (volumesPendentes <= 0)
        {
            _ = FinalizarExpedicaoAutomaticaAsync();
        }
    }

    private async Task FinalizarExpedicaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(_pedido.Id, "Aguardando Expediçao");

        if (sucesso)
        {
            await DisplayAlert("Expedição Concluída", "Todos os volumes foram expedidos com sucesso!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro ao Concluir", mensagem, "OK");
        }
    }
}