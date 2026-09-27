using SpringApp.Models;
using SpringApp.Services;
using Microsoft.Maui.Storage;

namespace SpringApp.Views;

public partial class MainPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private string _statusAtualFiltro = "Aguardando";
    private readonly int _perfilLogado;
    private List<Pedido> _pedidosAnteriores = new();

    public MainPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;

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

    private void AplicarRegrasDeAcessoPorPerfil()
    {
        if (_perfilLogado == 1)
        {
            AbaSeparacaoBtn.IsVisible = true;
            AbaConferenciaBtn.IsVisible = false;
            AbaExpedicaoBtn.IsVisible = true;
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
        else if (_perfilLogado == 2)
        {
            AbaSeparacaoBtn.IsVisible = false;
            AbaConferenciaBtn.IsVisible = true;
            AbaExpedicaoBtn.IsVisible = true;
            _statusAtualFiltro = "Aguardando Conferencia";
            AtualizarVisualAbas(AbaConferenciaBtn);
        }
        else
        {
            AbaSeparacaoBtn.IsVisible = true;
            AbaConferenciaBtn.IsVisible = true;
            AbaExpedicaoBtn.IsVisible = true;
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
    }

    private async void OnPerfilClicked(object sender, EventArgs e)
    {
        string nomePerfil = _perfilLogado switch
        {
            1 => "Separador",
            2 => "Conferente",
            3 => "Híbrido",
            4 => "Supervisor",
            _ => "Usuário Padrão"
        };

        string acao;
        string matriculaSalva = Preferences.Get("MatriculaLogada", "");
        if (_perfilLogado == 4 || matriculaSalva == "4" || matriculaSalva.Contains("supervisor"))
        {
            acao = await DisplayActionSheet($"Perfil: {nomePerfil}", "Cancelar", null, "Relatórios de Produtividade", "Sair da Conta");

            if (acao == "Relatórios de Produtividade")
            {
                var paginaSuper = Handler.MauiContext.Services.GetService<GestaoSupervisorPage>();
                await Navigation.PushAsync(paginaSuper);
                return;
            }
        }
        else
        {
            acao = await DisplayActionSheet($"Perfil: {nomePerfil}", "Cancelar", "Sair da Conta");
        }

        if (acao == "Sair da Conta")
        {
            Preferences.Remove("MatriculaLogada");
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }

    private async void OnSincronizarClicked(object sender, EventArgs e)
    {
        await ExecutarComLoadingAsync(SincronizarBtn, "Buscando...", async () =>
        {
            await CarregarPedidosAsync();
        });
    }

    private async Task CarregarPedidosAsync()
    {
        var pedidosAtivos = await _pedidoService.ObterPorStatusAsync(_statusAtualFiltro);
        PedidosCollectionView.ItemsSource = pedidosAtivos;

        var todosPedidos = await _pedidoService.GetPedidosAsync();

        if (_statusAtualFiltro == "Aguardando")
        {
            _pedidosAnteriores = todosPedidos.Where(p => p.Status != "Aguardando" && p.Status != "Em Separaçao").ToList();
        }
        else if (_statusAtualFiltro == "Aguardando Conferencia")
        {
            _pedidosAnteriores = todosPedidos.Where(p => p.Status == "Aguardando Expediçao" || p.Status == "Expedido" || p.Status == "Finalizado").ToList();
        }
        else if (_statusAtualFiltro == "Aguardando Expediçao")
        {
            _pedidosAnteriores = todosPedidos.Where(p => p.Status == "Expedido" || p.Status == "Finalizado").ToList();
        }

        BindableLayout.SetItemsSource(ListaPedidosAnteriores, _pedidosAnteriores);
    }

    private void OnTogglePedidosAnterioresTapped(object sender, TappedEventArgs e)
    {
        ListaPedidosAnteriores.IsVisible = !ListaPedidosAnteriores.IsVisible;
        SetaPedidosAnteriores.Text = ListaPedidosAnteriores.IsVisible ? "▲" : "▼";
    }

    private void OnPedidoAnteriorItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is Pedido pedidoTapped)
        {
            pedidoTapped.IsExpanded = !pedidoTapped.IsExpanded;
            BindableLayout.SetItemsSource(ListaPedidosAnteriores, null);
            BindableLayout.SetItemsSource(ListaPedidosAnteriores, _pedidosAnteriores);
        }
    }

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
        var mapaAbas = new (Button Botao, BoxView Linha)[]
        {
            (AbaSeparacaoBtn, LinhaSeparacao),
            (AbaConferenciaBtn, LinhaConferencia),
            (AbaExpedicaoBtn, LinhaExpedicao)
        };

        foreach (var item in mapaAbas)
        {
            if (item.Botao == abaAtiva)
            {
                item.Botao.TextColor = Color.FromArgb("#2563eb");
                item.Linha.Color = Color.FromArgb("#2563eb");
            }
            else
            {
                item.Botao.TextColor = Color.FromArgb("#64748b");
                item.Linha.Color = Colors.Transparent;
            }
        }
    }

    private async void OnIniciarAcaoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Pedido pedidoSelecionado)
        {
            await ExecutarComLoadingAsync(button, "Aguarde...", async () =>
            {
                if (pedidoSelecionado.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.IdPedido, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Separaçao";
                        await Navigation.PushAsync(new SeparacaoItensPage(pedidoSelecionado, _pedidoService));
                    }
                    else
                    {
                        await DisplayAlert("Erro", mensagem, "OK");
                    }
                }
                else if (pedidoSelecionado.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.IdPedido, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Conferencia";
                        await Navigation.PushAsync(new ConferenciaItensPage(pedidoSelecionado, _pedidoService));
                    }
                    else
                    {
                        await DisplayAlert("Erro", mensagem, "OK");
                    }
                }
                else if (pedidoSelecionado.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(new ExpedicaoItensPage(pedidoSelecionado, _pedidoService));
                }
            });
        }
    }

    private async Task ExecutarComLoadingAsync(Button botao, string textoLoading, Func<Task> acao)
    {
        if (!botao.IsEnabled) return;

        string textoOriginal = botao.Text;
        botao.Text = textoLoading;
        botao.IsEnabled = false;
        botao.Opacity = 0.7;

        try { await acao(); }
        finally
        {
            botao.Text = textoOriginal;
            botao.IsEnabled = true;
            botao.Opacity = 1.0;
        }
    }
}