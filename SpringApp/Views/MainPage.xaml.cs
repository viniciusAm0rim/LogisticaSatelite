using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class MainPage : ContentPage
{
    private readonly IPedidoService _pedidoService;
    private string _statusAtualFiltro = "Aguardando";
    private readonly int _perfilLogado;
    private List<Pedido> _pedidosEmEspera = new();
    private List<Pedido> _pedidosAtivos = new();

    // Evita que duas buscas na API rodem ao mesmo tempo
    private bool _carregandoPedidos;

    private string _termoPesquisadoAtual = string.Empty;
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public MainPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
        BindingContext = this;

        // Le o perfil salvo no login para saber quais abas mostrar
        string matriculaSalva = Preferences.Get("MatriculaLogada", "3");
        if (!int.TryParse(matriculaSalva, out _perfilLogado))
            _perfilLogado = 3;

        AplicarRegrasDeAcessoPorPerfil();
    }

    // Carrega os pedidos automaticamente sempre que esta tela aparece
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarPedidosAsync(viaPullToRefresh: false);
    }

    // Define quais abas ficam visiveis de acordo com o cargo do usuario
    private void AplicarRegrasDeAcessoPorPerfil()
    {
        if (_perfilLogado == 1)
        {
            ContainerAbaSeparacao.IsVisible = true;
            ContainerAbaConferencia.IsVisible = false;
            ContainerAbaExpedicao.IsVisible = true;
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
        else if (_perfilLogado == 2)
        {
            ContainerAbaSeparacao.IsVisible = false;
            ContainerAbaConferencia.IsVisible = true;
            ContainerAbaExpedicao.IsVisible = true;
            _statusAtualFiltro = "Aguardando Conferencia";
            AtualizarVisualAbas(AbaConferenciaBtn);
        }
        else
        {
            ContainerAbaSeparacao.IsVisible = true;
            ContainerAbaConferencia.IsVisible = true;
            ContainerAbaExpedicao.IsVisible = true;
            _statusAtualFiltro = "Aguardando";
            AtualizarVisualAbas(AbaSeparacaoBtn);
        }
    }

    // Abre o menu lateral expansivel (3 barras)
    private void OnMenuExpansivelClicked(object sender, EventArgs e)
    {
        OverlayMenuLateral.IsVisible = true;
    }

    // Fecha o menu lateral pelo botao X
    private void OnFecharMenuLateralClicked(object sender, EventArgs e)
    {
        OverlayMenuLateral.IsVisible = false;
    }

    // Fecha o menu lateral ao tocar fora dele
    private void OnFecharMenuLateralTapped(object sender, TappedEventArgs e)
    {
        OverlayMenuLateral.IsVisible = false;
    }

    // Abre a tela dedicada de Estimativas ao clicar no card do menu lateral
    private async void OnCardEstimativasMenuTapped(object sender, TappedEventArgs e)
    {
        OverlayMenuLateral.IsVisible = false;
        await Navigation.PushAsync(new EstimativasPage(_pedidoService, _perfilLogado));
    }

    // Abre a tela dedicada de Cotacoes ao clicar no card do menu lateral
    private async void OnCardCotacoesMenuTapped(object sender, TappedEventArgs e)
    {
        OverlayMenuLateral.IsVisible = false;
        await Navigation.PushAsync(new CotacoesPage(_pedidoService));
    }

    // Abre o menu do usuario com opcao de relatorio (supervisor) ou sair
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

    // Executado quando o usuario puxa a lista de pedidos para baixo
    private async void OnPedidosRefreshing(object sender, EventArgs e)
    {
        await CarregarPedidosAsync(viaPullToRefresh: true);
    }

    // Busca os pedidos e cotacoes na API e atualiza listas e contadores do menu
    private async Task CarregarPedidosAsync(bool viaPullToRefresh = false)
    {
        if (_carregandoPedidos)
        {
            if (viaPullToRefresh)
                PedidosRefreshView.IsRefreshing = false;
            return;
        }

        _carregandoPedidos = true;

        if (!viaPullToRefresh)
            ExibirSkeleton(true);

        PesquisaExpedicaoBar.Text = string.Empty;

        try
        {
            var todosPedidos = await _pedidoService.GetPedidosAsync();
            var cotacoesPendentes = await _pedidoService.GetCotacoesPendentesAsync();

            BannerConexao.IsVisible = false;

            _pedidosAtivos = todosPedidos
                .Where(p => !p.EmEspera && string.Equals(p.Status, _statusAtualFiltro, StringComparison.OrdinalIgnoreCase))
                .ToList();

            PedidosCollectionView.ItemsSource = _pedidosAtivos;
            AtualizarPedidosEmEspera(todosPedidos);
            AtualizarPendenciasMenuLateral(todosPedidos, cotacoesPendentes.Count);
        }
        catch (TaskCanceledException)
        {
            LabelErroConexao.Text = "Tempo limite esgotado ao conectar na API. Puxe para baixo para tentar novamente.";
            BannerConexao.IsVisible = true;
        }
        catch (HttpRequestException)
        {
            LabelErroConexao.Text = "Falha de conexão com a API. Verifique a rede e puxe para baixo.";
            BannerConexao.IsVisible = true;
        }
        catch (Exception)
        {
            LabelErroConexao.Text = "Não foi possível atualizar os pedidos no momento.";
            BannerConexao.IsVisible = true;
        }
        finally
        {
            ExibirSkeleton(false);
            PedidosRefreshView.IsRefreshing = false;
            _carregandoPedidos = false;
        }
    }

    // Atualiza o contador das 3 barras e dos cards Estimativas e Cotacoes no menu lateral
    private void AtualizarPendenciasMenuLateral(List<Pedido> todosPedidos, int totalCotacoesPendentes)
    {
        int totalEstimativas = todosPedidos.Count(p =>
        {
            bool emExecucao = p.Status.Contains("Em Separa", StringComparison.OrdinalIgnoreCase) ||
                              p.Status.Contains("Em Confer", StringComparison.OrdinalIgnoreCase);

            if (emExecucao) return false;

            if (_perfilLogado == 1)
                return p.EstimativaPendenteSeparacao;

            if (_perfilLogado == 2)
                return p.EstimativaPendenteConferencia;

            return p.EstimativaPendenteSeparacao || p.EstimativaPendenteConferencia;
        });

        int totalGeral = totalEstimativas + totalCotacoesPendentes;

        BadgeMenuLabel.Text = totalGeral.ToString();
        BadgeMenuContainer.IsVisible = totalGeral > 0;

        ContadorEstimativasMenuLabel.Text = totalEstimativas.ToString();
        BadgeCardEstimativas.IsVisible = totalEstimativas > 0;

        ContadorCotacoesMenuLabel.Text = totalCotacoesPendentes.ToString();
        BadgeCardCotacoes.IsVisible = totalCotacoesPendentes > 0;
    }

    private void ExibirSkeleton(bool visivel)
    {
        SkeletonLoadingView.IsVisible = visivel;
        PedidosRefreshView.IsVisible = !visivel;
    }

    private void AtualizarPedidosEmEspera(List<Pedido> todosPedidos)
    {
        string etapaFiltro = _statusAtualFiltro switch
        {
            "Aguardando" => "Separação",
            "Aguardando Conferencia" => "Conferência",
            "Aguardando Expediçao" => "Expedição",
            _ => string.Empty
        };

        _pedidosEmEspera = todosPedidos
            .Where(p => p.EmEspera && p.EtapaPausada.Equals(etapaFiltro, StringComparison.OrdinalIgnoreCase))
            .ToList();

        CardPedidosEmEspera.IsVisible = _pedidosEmEspera.Any();
        ContadorEsperaLabel.Text = _pedidosEmEspera.Count.ToString();
        BindableLayout.SetItemsSource(ListaPedidosEmEspera, null);
        BindableLayout.SetItemsSource(ListaPedidosEmEspera, _pedidosEmEspera);
    }

    private async void OnRetomarPedidoEsperaClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Pedido pedidoPausado)
        {
            await ExecutarComLoadingAsync(button, "Aguarde...", async () =>
            {
                var (sucesso, mensagem) = await _pedidoService.RetomarPedidoAsync(pedidoPausado.IdPedido, _usuarioLogado);
                if (!sucesso)
                {
                    await DisplayAlert("Erro", mensagem, "OK");
                    return;
                }

                if (pedidoPausado.EtapaPausada.Equals("Separação", StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(new SeparacaoItensPage(pedidoPausado, _pedidoService));
                }
                else if (pedidoPausado.EtapaPausada.Equals("Conferência", StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(new ConferenciaItensPage(pedidoPausado, _pedidoService));
                }
                else if (pedidoPausado.EtapaPausada.Equals("Expedição", StringComparison.OrdinalIgnoreCase))
                {
                    await Navigation.PushAsync(new ExpedicaoItensPage(pedidoPausado, _pedidoService));
                }
            });
        }
    }

    private async void OnAbaClicked(object sender, EventArgs e)
    {
        if (sender is Button abaClicada && abaClicada.CommandParameter is string status)
        {
            _statusAtualFiltro = status;
            AtualizarVisualAbas(abaClicada);

            PesquisaExpedicaoBar.IsVisible = status == "Aguardando Expediçao";

            await CarregarPedidosAsync(viaPullToRefresh: false);
        }
    }

    private async void OnPesquisaExpedicaoPesquisar(object sender, EventArgs e)
    {
        string termo = PesquisaExpedicaoBar.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(termo))
        {
            PedidosCollectionView.ItemsSource = _pedidosAtivos;
            return;
        }

        _termoPesquisadoAtual = termo;

        var todosCliente = await _pedidoService.ObterPorClienteAsync(termo);
        var pendentes = todosCliente.Where(p =>
            !p.Status.Equals("Expedido", StringComparison.OrdinalIgnoreCase) &&
            !p.Status.Equals("Finalizado", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        bool temOutrosStatus = pendentes.Any(p => !p.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase));

        if (temOutrosStatus)
        {
            LabelTermoPesquisado.Text = $"Busca: \"{termo}\"";
            OverlayPesquisaCliente.IsVisible = true;
        }
        else
        {
            var expedicaoPedidos = pendentes.Where(p => !p.EmEspera && p.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase)).ToList();
            PedidosCollectionView.ItemsSource = expedicaoPedidos;
        }
    }

    private void OnOpcaoPesquisaApenasExpedicaoClicked(object sender, EventArgs e)
    {
        OverlayPesquisaCliente.IsVisible = false;
        string termo = _termoPesquisadoAtual.ToLower();

        var filtrados = _pedidosAtivos.Where(p =>
            (p.Cliente != null && p.Cliente.ToLower().Contains(termo)) ||
            p.IdPedido.ToString() == termo
        ).ToList();

        PedidosCollectionView.ItemsSource = filtrados;
    }

    private async void OnOpcaoPesquisaTodosPedidosClicked(object sender, EventArgs e)
    {
        OverlayPesquisaCliente.IsVisible = false;
        await Navigation.PushAsync(new PedidosClientePage(_termoPesquisadoAtual, _pedidoService));
    }

    private void OnFecharOverlayPesquisaClicked(object sender, EventArgs e)
    {
        OverlayPesquisaCliente.IsVisible = false;
    }

    private void OnPesquisaExpedicaoTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            PedidosCollectionView.ItemsSource = _pedidosAtivos;
        }
    }

    private void AtualizarVisualAbas(Button abaAtiva)
    {
        var mapaAbas = new (Button Botao, Border Pill, Label Icone, Label Texto)[]
        {
            (AbaSeparacaoBtn, PillSeparacao, IconSeparacao, TextSeparacao),
            (AbaConferenciaBtn, PillConferencia, IconConferencia, TextConferencia),
            (AbaExpedicaoBtn, PillExpedicao, IconExpedicao, TextExpedicao)
        };

        foreach (var item in mapaAbas)
        {
            item.Icone.TextColor = Colors.White;
            item.Texto.TextColor = Colors.White;

            if (abaAtiva != null && item.Botao == abaAtiva)
            {
                item.Pill.BackgroundColor = Color.FromArgb("#475569");
            }
            else
            {
                item.Pill.BackgroundColor = Colors.Transparent;
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
                    else await DisplayAlert("Erro", mensagem, "OK");
                }
                else if (pedidoSelecionado.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, mensagem) = await _pedidoService.AvancarEtapaAsync(pedidoSelecionado.IdPedido, pedidoSelecionado.Status);
                    if (sucesso)
                    {
                        pedidoSelecionado.Status = "Em Conferencia";
                        await Navigation.PushAsync(new ConferenciaItensPage(pedidoSelecionado, _pedidoService));
                    }
                    else await DisplayAlert("Erro", mensagem, "OK");
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