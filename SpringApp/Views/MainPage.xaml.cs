using SpringApp.Models;
using SpringApp.Services;
using Microsoft.Maui.Storage;

namespace SpringApp.Views;

public partial class MainPage : ContentPage
{
    private readonly PedidoService _pedidoService;
    private string _statusAtualFiltro = "Aguardando";
    private readonly int _perfilLogado;

    public MainPage()
    {
        InitializeComponent();
        _pedidoService = new PedidoService();

        string matriculaSalva = Preferences.Get("MatriculaLogada", "3");

        if (!int.TryParse(matriculaSalva, out _perfilLogado))
        {
            _perfilLogado = 3;
        }

        AplicarRegrasDeAcessoPorPerfil();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarPedidosAsync();
    }

    // --- REGRAS DE NEGÓCIO DOS PERFIS ---
    private void AplicarRegrasDeAcessoPorPerfil()
    {
        AbasGrid.ColumnDefinitions.Clear();

        if (_perfilLogado == 1)
        {
            ConfigurarAba(AbaSeparacaoBtn, true);
            ConfigurarAba(AbaConferenciaBtn, false);
            ConfigurarAba(AbaAbastecimentoBtn, true);
            ConfigurarAba(AbaExpedicaoBtn, true);
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
        else if (_perfilLogado == 2)
        {
            ConfigurarAba(AbaSeparacaoBtn, false);
            ConfigurarAba(AbaConferenciaBtn, true);
            ConfigurarAba(AbaAbastecimentoBtn, true);
            ConfigurarAba(AbaExpedicaoBtn, true);
            _statusAtualFiltro = "Aguardando Conferencia";
            AtualizarVisualAbas(AbaConferenciaBtn);
        }
        else
        {
            ConfigurarAba(AbaSeparacaoBtn, true);
            ConfigurarAba(AbaConferenciaBtn, true);
            ConfigurarAba(AbaAbastecimentoBtn, true);
            ConfigurarAba(AbaExpedicaoBtn, true);
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
    }

    private void ConfigurarAba(Button aba, bool visivel)
    {
        aba.IsVisible = visivel;
        if (visivel)
        {
            Grid.SetColumn(aba, AbasGrid.ColumnDefinitions.Count);
            AbasGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }
    }

    // --- MENU DE PERFIL ---
    private async void OnPerfilTapped(object sender, TappedEventArgs e)
    {
        string nomePerfil = _perfilLogado switch
        {
            1 => "Separador",
            2 => "Conferente",
            3 => "Híbrido",
            4 => "Supervisor",
            _ => "Usuário Padrão"
        };

        string acao = await DisplayActionSheet($"Perfil Ativo: {nomePerfil}", "Cancelar", "Sair da Conta");

        if (acao == "Sair da Conta")
        {
            Preferences.Remove("MatriculaLogada");
            Application.Current.MainPage = new LoginPage();
        }
    }

    // --- SINCRONIZAÇÃO ---
    private async void OnSincronizarClicked(object sender, EventArgs e)
    {
        await ExecutarComLoadingAsync(SincronizarBtn, "Buscando...", async () =>
        {
            await Task.Delay(1500);
            await CarregarPedidosAsync();
        });
    }

    // --- GERENCIAMENTO DA LISTA ---
    private async Task CarregarPedidosAsync()
    {
        var pedidos = await _pedidoService.ObterPorStatusAsync(_statusAtualFiltro);
        PedidosCollectionView.ItemsSource = pedidos;
        AtualizarRefreshView.IsRefreshing = false;
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await CarregarPedidosAsync();
    }

    // --- NAVEGAÇÃO ENTRE ABAS ---
    private async void OnAbaClicked(object sender, EventArgs e)
    {
        if (sender is Button abaClicada && abaClicada.CommandParameter is string status)
        {
            _statusAtualFiltro = status;
            AtualizarVisualAbas(abaClicada);

            PedidosCollectionView.ItemsSource = null;
            await CarregarPedidosAsync();
        }
    }

    private void AtualizarVisualAbas(Button abaAtiva)
    {
        Button[] abas = { AbaSeparacaoBtn, AbaConferenciaBtn, AbaAbastecimentoBtn, AbaExpedicaoBtn };

        foreach (var aba in abas)
        {
            if (aba == abaAtiva)
            {
                aba.BackgroundColor = Color.FromArgb("#004aad");
                aba.TextColor = Colors.White;
            }
            else
            {
                aba.BackgroundColor = Color.FromArgb("#e2e8f0");
                aba.TextColor = Color.FromArgb("#475569");
            }
        }
    }

    // --- TRANSIÇÃO DE PEDIDOS ---
    private async void OnIniciarAcaoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Pedido pedidoSelecionado)
        {
            await ExecutarComLoadingAsync(button, "Aguarde...", async () =>
            {
                // Separação e Conferência alteram o status logo ao iniciar
                if (pedidoSelecionado.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.Id, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Separaçao";
                        await Navigation.PushAsync(new SeparacaoItensPage(pedidoSelecionado));
                    }
                    else
                    {
                        await DisplayAlert("Erro", mensagem, "OK");
                    }
                }
                else if (pedidoSelecionado.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.Id, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Conferencia";
                        await Navigation.PushAsync(new ConferenciaItensPage(pedidoSelecionado));
                    }
                    else
                    {
                        await DisplayAlert("Erro", mensagem, "OK");
                    }
                }
                // Expedição apenas navega, pois a alteração para "Expedido" ocorre após baixar os volumes na tela seguinte
                else if (pedidoSelecionado.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(new ExpedicaoItensPage(pedidoSelecionado));
                }
                else
                {
                    await CarregarPedidosAsync();
                }
            });
        }
    }

    // --- EFEITOS VISUAIS DE LOADING ---
    private async Task ExecutarComLoadingAsync(Button botao, string textoLoading, Func<Task> acao)
    {
        if (!botao.IsEnabled) return;

        string textoOriginal = botao.Text;
        botao.Text = textoLoading;
        botao.IsEnabled = false;
        botao.Opacity = 0.7;

        try
        {
            await acao();
        }
        finally
        {
            botao.Text = textoOriginal;
            botao.IsEnabled = true;
            botao.Opacity = 1.0;
        }
    }
}