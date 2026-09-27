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
    private readonly Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private int _errosEvitados = 0; // <-- Variável para acumular os erros da expedição
    private List<VolumeExpedicao> _listaVolumes;

    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public ExpedicaoItensPage(Pedido pedido, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedido = pedido;
        _pedidoService = pedidoService;
        _listaVolumes = new List<VolumeExpedicao>();

        CarregarDados();
        _ = GerarVolumesDoBancoAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _pedidoService.BloquearPedidoAsync(_pedido.IdPedido, _usuarioLogado);
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    protected override bool OnBackButtonPressed() => true;

    private void CarregarDados()
    {
        NumeroPedidoLabel.Text = $"#{_pedido.IdPedido:D4}";
        ClienteLabel.Text = (_pedido.Cliente ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();
    }

    private async Task GerarVolumesDoBancoAsync()
    {
        _listaVolumes.Clear();
        var pedidoAtualizado = await _pedidoService.GetPedidoPorIdAsync(_pedido.IdPedido);

        if (pedidoAtualizado?.VolumesPedidos != null && pedidoAtualizado.VolumesPedidos.Any())
        {
            int i = 1;
            foreach (var vol in pedidoAtualizado.VolumesPedidos)
            {
                _listaVolumes.Add(new VolumeExpedicao
                {
                    CodigoEsperado = vol.CodigoVolume,
                    ExibicaoNome = $"Volume: {vol.CodigoVolume}",
                    Bipado = vol.BipadoExpedicao
                });
                i++;
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
                _errosEvitados++; // <-- Tentou bipar volume já bipado
                DisplayAlert("Aviso", "Este volume já foi lido!", "OK");
            }
        }
        else
        {
            _errosEvitados++; // <-- Tentou bipar volume incorreto ou que não pertence ao pedido
            DisplayAlert("Erro", $"Código '{codigo}' não pertence a este pedido.", "OK");
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
        if (confirm) await Navigation.PopAsync();
    }

    private async Task FinalizarExpedicaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        // Envia os erros acumulados na expedição junto com a conclusão
        var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(_pedido.IdPedido, _pedido.Status, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Expedição concluída!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", msg, "OK");
        }
    }
}