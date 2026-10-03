using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public class VolumeExpedicao
{
    public string CodigoEsperado { get; set; }
    public string ExibicaoNome { get; set; }
    public bool Bipado { get; set; }
    public string CorFundo => Bipado ? "#dcfce7" : "#f1f5f9";
    public string CorTexto => Bipado ? "#15803d" : "#64748b";
    public string StatusTexto => Bipado ? "LIDO ✓" : "AGUARDAR";
}

public partial class ExpedicaoItensPage : ContentPage
{
    // Agora gerimos uma LISTA de pedidos (que pode conter 1 ou vários em caso de lote)
    private readonly List<Pedido> _pedidosLote;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private int _errosEvitados = 0;
    private List<VolumeExpedicao> _listaVolumes;

    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    // Construtor original (quando clica em apenas 1 pedido na lista)
    public ExpedicaoItensPage(Pedido pedido, IPedidoService pedidoService)
        : this(new List<Pedido> { pedido }, pedidoService) { }

    // Novo construtor (quando escolhe expedir em lote via barra de pesquisa)
    public ExpedicaoItensPage(List<Pedido> pedidosLote, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidosLote = pedidosLote;
        _pedidoService = pedidoService;
        _listaVolumes = new List<VolumeExpedicao>();

        CarregarDados();
        _ = GerarVolumesDoBancoAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Bloqueia todos os pedidos do lote no banco
        foreach (var p in _pedidosLote)
        {
            await _pedidoService.BloquearPedidoAsync(p.IdPedido, _usuarioLogado);
        }
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    protected override bool OnBackButtonPressed() => true;

    private void CarregarDados()
    {
        if (_pedidosLote.Count == 1)
        {
            NumeroPedidoLabel.Text = $"#{_pedidosLote[0].IdPedido:D4}";
            ClienteLabel.Text = (_pedidosLote[0].Cliente ?? "").ToUpper();
            ModalLabel.Text = string.IsNullOrEmpty(_pedidosLote[0].Modal) ? "PADRÃO" : _pedidosLote[0].Modal.ToUpper();
        }
        else
        {
            NumeroPedidoLabel.Text = $"LOTE: {_pedidosLote.Count} PEDIDOS";
            // Pressupõe que a filtragem do lote foi por cliente
            ClienteLabel.Text = (_pedidosLote[0].Cliente ?? "").ToUpper();
            ModalLabel.Text = "VÁRIOS MODAIS";
        }
    }

    private async Task GerarVolumesDoBancoAsync()
    {
        _listaVolumes.Clear();

        // Puxa e aglomera os volumes de TODOS os pedidos do lote
        foreach (var p in _pedidosLote)
        {
            var pedidoAtualizado = await _pedidoService.GetPedidoPorIdAsync(p.IdPedido);

            if (pedidoAtualizado?.VolumesPedidos != null && pedidoAtualizado.VolumesPedidos.Any())
            {
                foreach (var vol in pedidoAtualizado.VolumesPedidos)
                {
                    _listaVolumes.Add(new VolumeExpedicao
                    {
                        CodigoEsperado = vol.CodigoVolume,
                        ExibicaoNome = $"Pedido #{p.IdPedido} | Vol: {vol.CodigoVolume}", // Ajuda visual para o operador saber de qual pedido é a caixa
                        Bipado = vol.BipadoExpedicao
                    });
                }
            }
        }
        AtualizarContadores();
    }

    private void OnCodigoScaneadoCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.Text))
        {
            string codigo = entry.Text.Trim();
            entry.Text = string.Empty;
            ProcessarBipagemVolume(codigo);
            Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
        }
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        string codigo = await DisplayPromptAsync("Digitação Manual", "Informe o código do Volume:", "Confirmar", "Cancelar");
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            ProcessarBipagemVolume(codigo.Trim());
        }
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    private void ProcessarBipagemVolume(string codigo)
    {
        var volumeEncontrado = _listaVolumes.FirstOrDefault(v => v.CodigoEsperado.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        if (volumeEncontrado != null)
        {
            if (!volumeEncontrado.Bipado)
            {
                volumeEncontrado.Bipado = true;
                AtualizarContadores();
            }
            else
            {
                _errosEvitados++;
                DisplayAlert("Aviso", "Este volume já foi lido!", "OK");
            }
        }
        else
        {
            _errosEvitados++;
            DisplayAlert("Erro", $"Código '{codigo}' não pertence a nenhum pedido deste lote.", "OK");
        }
    }

    private void AtualizarContadores()
    {
        int totalVolumes = _listaVolumes.Count;
        int volumesBipados = _listaVolumes.Count(v => v.Bipado);
        int pendente = totalVolumes - volumesBipados;

        VolumesRestantesLabel.Text = volumesBipados.ToString();
        VolumesTotaisLabel.Text = totalVolumes.ToString();
        NumeroVolumeGrandeLabel.Text = Math.Max(0, pendente).ToString();

        VolumesCollectionView.ItemsSource = null;
        VolumesCollectionView.ItemsSource = _listaVolumes;

        if (pendente <= 0 && totalVolumes > 0)
        {
            _ = FinalizarExpedicaoAutomaticaAsync();
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Atenção", "Deseja sair da expedição?", "Sim", "Não");
        if (confirm)
        {
            // Em caso de cancelamento, desfaz/reverte os status de todos do lote
            foreach (var p in _pedidosLote)
            {
                await _pedidoService.ReverterEtapaAsync(p.IdPedido);
            }
            await Navigation.PopAsync();
        }
    }

    private async Task FinalizarExpedicaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        bool sucessoTotal = true;
        string mensagemErro = "";

        // Mantém a métrica precisa enviando os erros evitados apenas no primeiro pedido do lote
        int errosParaEnviar = _errosEvitados;

        foreach (var p in _pedidosLote)
        {
            var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(p.IdPedido, p.Status, _usuarioLogado, errosParaEnviar);
            errosParaEnviar = 0; // Zera para as próximas iterações para não duplicar o contador de falhas na API

            if (!sucesso)
            {
                sucessoTotal = false;
                mensagemErro = msg;
                break;
            }
        }

        if (sucessoTotal)
        {
            string titulo = _pedidosLote.Count > 1 ? "Expedição do Lote concluída!" : "Expedição concluída!";
            await DisplayAlert("Sucesso", titulo, "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", $"Falha ao baixar um dos pedidos do lote: {mensagemErro}", "OK");
        }
    }
}