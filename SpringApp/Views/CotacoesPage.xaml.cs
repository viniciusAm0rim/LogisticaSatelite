using System.Globalization;
using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class CotacoesPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private Cotacao? _cotacaoSelecionada;
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public CotacoesPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarCotacoesPendentesAsync();
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CarregarCotacoesPendentesAsync();
        CotacoesRefreshView.IsRefreshing = false;
    }

    private async Task CarregarCotacoesPendentesAsync()
    {
        try
        {
            var pendentes = await _pedidoService.GetCotacoesPendentesAsync();
            CotacoesCollectionView.ItemsSource = pendentes;
        }
        catch (Exception)
        {
            await DisplayAlert("Erro", "Não foi possível carregar as cotações pendentes.", "OK");
        }
    }

    private void OnAbrirFichaCotacaoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Cotacao cotacao)
        {
            _cotacaoSelecionada = cotacao;
            LabelTituloCotacao.Text = $"Cotação #{cotacao.Id:D4} - {cotacao.Cliente}";

            if (!string.IsNullOrWhiteSpace(cotacao.Observacao))
            {
                LabelObsCotacao.Text = $"Obs: {cotacao.Observacao}";
                BoxObsCotacao.IsVisible = true;
            }
            else
            {
                BoxObsCotacao.IsVisible = false;
            }

            ItensCotacaoView.ItemsSource = cotacao.Itens;
            VolumesEntry.Text = string.Empty;
            PesoEntry.Text = string.Empty;
            DimensoesEntry.Text = string.Empty;

            OverlayCotacao.IsVisible = true;
        }
    }

    private void OnCancelarCotacaoClicked(object sender, EventArgs e)
    {
        OverlayCotacao.IsVisible = false;
        _cotacaoSelecionada = null;
    }

    private async void OnConfirmarCotacaoClicked(object sender, EventArgs e)
    {
        if (_cotacaoSelecionada == null) return;

        if (!int.TryParse(VolumesEntry.Text?.Trim(), out int volumes) || volumes <= 0)
        {
            await DisplayAlert("Atenção", "Informe uma quantidade válida de volumes.", "OK");
            return;
        }

        string pesoTexto = (PesoEntry.Text ?? string.Empty).Trim().Replace(',', '.');
        if (!double.TryParse(pesoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out double pesoKg) || pesoKg <= 0)
        {
            await DisplayAlert("Atenção", "Informe um peso total válido em kg.", "OK");
            return;
        }

        string dimensoes = DimensoesEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(dimensoes))
        {
            await DisplayAlert("Atenção", "Informe as dimensões / cubagem.", "OK");
            return;
        }

        OverlayCotacao.IsVisible = false;

        var (sucesso, mensagem) = await _pedidoService.ResponderCotacaoAsync(
            _cotacaoSelecionada.Id,
            volumes,
            pesoKg,
            dimensoes,
            _usuarioLogado);

        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Cotação enviada ao comercial!", "OK");
            await CarregarCotacoesPendentesAsync();
        }
        else
        {
            await DisplayAlert("Erro", $"Falha ao enviar cotação: {mensagem}", "OK");
        }

        _cotacaoSelecionada = null;
    }
}