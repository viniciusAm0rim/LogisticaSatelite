using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class SeparacaoItensPage : ContentPage
{
    private readonly Pedido _pedido;
    private readonly PedidoService _pedidoService;
    private bool _isFinalizando = false;

    public SeparacaoItensPage(Pedido pedido)
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
        AtualizarContadores();
    }

    private void OnBiparClicked(object sender, EventArgs e) => ExecutarBipagemPorCodigo(CodigoBarraEntry.Text?.Trim());
    private void OnBiparCompleted(object sender, EventArgs e) => ExecutarBipagemPorCodigo(CodigoBarraEntry.Text?.Trim());
    private void OnBiparItemDiretoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ItemPedido item)
        {
            ProcessarBipagemItem(item);
        }
    }

    private void ExecutarBipagemPorCodigo(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return;

        var item = _pedido.Itens.FirstOrDefault(i =>
            i.Nome.Equals(codigo, StringComparison.OrdinalIgnoreCase) ||
            i.Id.ToString() == codigo);

        if (item != null)
        {
            ProcessarBipagemItem(item);
        }
        else
        {
            DisplayAlert("Atenção", "Item não pertence a este pedido!", "OK");
        }

        CodigoBarraEntry.Text = string.Empty;
        CodigoBarraEntry.Focus();
    }

    private void ProcessarBipagemItem(ItemPedido item)
    {
        if (item.BipagensSeparacao < item.Quantidade)
        {
            item.BipagensSeparacao++;
            AtualizarContadores();
        }
        else
        {
            DisplayAlert("Aviso", "A quantidade deste item já foi totalmente bipada!", "OK");
        }
    }

    private void AtualizarContadores()
    {
        int totalItensGeral = _pedido.Itens.Sum(i => i.Quantidade);
        int totalSeparadosGeral = _pedido.Itens.Sum(i => i.BipagensSeparacao);
        int totalPendente = _pedido.Itens.Sum(i => i.SeparacaoPendente);

        TotalRestanteLabel.Text = $"{totalSeparadosGeral} de {totalItensGeral}";

        ItensCollectionView.ItemsSource = null;
        ItensCollectionView.ItemsSource = _pedido.Itens;

        if (totalPendente <= 0 && _pedido.Itens.Any())
        {
            _ = FinalizarSeparacaoAutomaticaAsync();
        }
    }

    private async Task FinalizarSeparacaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(_pedido.Id, "Em Separaçao");

        if (sucesso)
        {
            await DisplayAlert("Separação Concluída", "Todos os itens foram bipados! Pedido enviado para conferência.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro ao Concluir", mensagem, "OK");
        }
    }
}