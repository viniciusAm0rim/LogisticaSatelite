using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class AbastecimentoPage : ContentPage
{
    private readonly PedidoService _pedidoService;

    // Mude de List<ItemPedido> para List<Produto>
    private List<Produto> _itensRecomendados = new();

    public AbastecimentoPage()
    {
        InitializeComponent();
        _pedidoService = new PedidoService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarRecomendacoesAsync();
    }

    private async Task CarregarRecomendacoesAsync()
    {
        _itensRecomendados = await _pedidoService.ObterRecomendacoesAbastecimentoAsync();
        TotalCriticosLabel.Text = $"{_itensRecomendados.Count} itens";
        RecomendacoesCollectionView.ItemsSource = _itensRecomendados;
    }

    private async void OnVoltarTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnAbastecerClicked(object sender, EventArgs e)
    {
        // Mude de ItemPedido para Produto no cast do CommandParameter
        if (sender is Button button && button.CommandParameter is Produto produto)
        {
            string codigoBipado = await DisplayPromptAsync(
                "Confirmar Abastecimento",
                $"Vá até o pulmão {produto.LocalizacaoPulmaoFormatada}\nBipe a caixa do produto (SKU: {produto.CodItem}):",
                accept: "Confirmar Reposição",
                cancel: "Cancelar",
                placeholder: "Digite ou bipe o código...",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrEmpty(codigoBipado)) return;

            if (codigoBipado.Trim().Equals(produto.CodItem.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                string matriculaLogada = Preferences.Get("MatriculaLogada", "1");

                var (sucesso, mensagem) = await _pedidoService.ConfirmarAbastecimentoAsync(produto.Id, matriculaLogada);

                if (sucesso)
                {
                    await DisplayAlert("Sucesso", $"Item {produto.Nome} abastecido com sucesso!", "OK");
                    await CarregarRecomendacoesAsync();
                }
                else
                {
                    await DisplayAlert("Erro", mensagem, "OK");
                }
            }
            else
            {
                await DisplayAlert("Atenção", "Código de barras incorreto! A caixa bipada não corresponde a este produto.", "OK");
            }
        }
    }
}