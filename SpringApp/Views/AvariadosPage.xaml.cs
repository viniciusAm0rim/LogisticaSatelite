using SpringApp.Services;

namespace SpringApp.Views;

public partial class AvariadosPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "Operador");

    public AvariadosPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAvariadosAsync();
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CarregarAvariadosAsync();
        AvariadosRefreshView.IsRefreshing = false;
    }

    private async Task CarregarAvariadosAsync()
    {
        var lista = await _pedidoService.GetProdutosAvariadosAsync();
        AvariadosCollectionView.ItemsSource = lista;
    }

    private void OnAbrirModalRegistrarAvariaClicked(object sender, EventArgs e)
    {
        SkuAvariaEntry.Text = string.Empty;
        QtdAvariaEntry.Text = "1";
        MotivoAvariaEntry.Text = string.Empty;
        OverlayRegistrarAvaria.IsVisible = true;
    }

    private void OnCancelarAvariaClicked(object sender, EventArgs e)
    {
        OverlayRegistrarAvaria.IsVisible = false;
    }

    private async void OnConfirmarAvariaClicked(object sender, EventArgs e)
    {
        string sku = SkuAvariaEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(sku))
        {
            await DisplayAlert("Atenção", "Informe ou bipe o SKU do produto.", "OK");
            return;
        }

        if (!int.TryParse(QtdAvariaEntry.Text?.Trim(), out int qtd) || qtd <= 0)
        {
            await DisplayAlert("Atenção", "Informe uma quantidade válida maior que zero.", "OK");
            return;
        }

        var (sucesso, msg) = await _pedidoService.RegistrarAvariaEstoqueAsync(
            sku, qtd, MotivoAvariaEntry.Text ?? string.Empty, _usuarioLogado);

        if (!sucesso)
        {
            await DisplayAlert("Erro", msg, "OK");
            return;
        }

        OverlayRegistrarAvaria.IsVisible = false;
        await DisplayAlert("Registrado", msg, "OK");
        await CarregarAvariadosAsync();
    }
}