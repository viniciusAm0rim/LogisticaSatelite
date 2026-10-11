using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public class PedidoViewModelWrapper
{
    public Pedido PedidoOriginal { get; set; }
    public int IdPedido => PedidoOriginal.IdPedido;
    public string Cliente => PedidoOriginal.Cliente;
    public string Modal => PedidoOriginal.Modal;
    public string Status => PedidoOriginal.Status;

    // Nomes curtos e padronizados para manter o mesmo tamanho visual do botão
    public string BotaoTextoAcao => Status switch
    {
        "Aguardando" or "Em Separaçao" => "Separar",
        "Aguardando Conferencia" or "Em Conferencia" => "Conferir",
        "Aguardando Expediçao" => "Expedir",
        _ => "Iniciar"
    };

    public string BotaoCorAcao => Status switch
    {
        "Aguardando" or "Em Separaçao" => "#2563eb",
        "Aguardando Conferencia" or "Em Conferencia" => "#10b981",
        "Aguardando Expediçao" => "#f59e0b",
        _ => "#64748b"
    };

    public bool BotaoHabilitadoAcao =>
        !string.Equals(Status?.Trim(), "Expedido", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Status?.Trim(), "Finalizado", StringComparison.OrdinalIgnoreCase);
}

public partial class PedidosClientePage : ContentPage
{
    private readonly string _termoCliente;
    private readonly IPedidoService _pedidoService;

    public PedidosClientePage(string termoCliente, IPedidoService pedidoService)
    {
        InitializeComponent();
        _termoCliente = termoCliente;
        _pedidoService = pedidoService;
        NomeClienteLabel.Text = _termoCliente.ToUpper();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarTodosPedidosDoClienteAsync();
    }

    private async Task CarregarTodosPedidosDoClienteAsync()
    {
        var listaPedidos = await _pedidoService.ObterPorClienteAsync(_termoCliente);

        // Filtro estrito: Omitir qualquer pedido ja expedido ou finalizado
        var pendentes = listaPedidos.Where(p =>
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Status)) return false;

            string st = p.Status.Trim();

            bool jaExpedidoOuFinalizado =
                st.Equals("Expedido", StringComparison.OrdinalIgnoreCase) ||
                st.Equals("Finalizado", StringComparison.OrdinalIgnoreCase) ||
                st.Equals("Concluido", StringComparison.OrdinalIgnoreCase) ||
                (st.Contains("Expedi", StringComparison.OrdinalIgnoreCase) && !st.Contains("Aguardando", StringComparison.OrdinalIgnoreCase));

            return !jaExpedidoOuFinalizado;
        }).ToList();

        var wrappers = pendentes.Select(p => new PedidoViewModelWrapper
        {
            PedidoOriginal = p
        }).ToList();

        PedidosCollectionView.ItemsSource = wrappers;
    }

    private async void OnAcaoPedidoClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PedidoViewModelWrapper wrapper)
        {
            var p = wrapper.PedidoOriginal;

            if (p.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("Em Separaçao", StringComparison.OrdinalIgnoreCase))
            {
                if (p.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(p.IdPedido, p.Status);
                    if (sucesso) p.Status = "Em Separaçao";
                    else { await DisplayAlert("Erro", msg, "OK"); return; }
                }
                await Navigation.PushAsync(new SeparacaoItensPage(p, _pedidoService));
            }
            else if (p.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("Em Conferencia", StringComparison.OrdinalIgnoreCase))
            {
                if (p.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase))
                {
                    var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(p.IdPedido, p.Status);
                    if (sucesso) p.Status = "Em Conferencia";
                    else { await DisplayAlert("Erro", msg, "OK"); return; }
                }
                await Navigation.PushAsync(new ConferenciaItensPage(p, _pedidoService));
            }
            else if (p.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase))
            {
                await Navigation.PushAsync(new ExpedicaoItensPage(p, _pedidoService));
            }
        }
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}