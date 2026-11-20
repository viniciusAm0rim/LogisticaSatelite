using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SpringApp.Helpers;
using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public class VolumeExpedicao : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public int PedidoId { get; set; }
    public string CodigoEsperado { get; set; } = string.Empty;
    public string ExibicaoNome { get; set; } = string.Empty;

    private bool _bipado;
    public bool Bipado
    {
        get => _bipado;
        set
        {
            if (_bipado != value)
            {
                _bipado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorFundo));
                OnPropertyChanged(nameof(CorTexto));
                OnPropertyChanged(nameof(StatusTexto));
            }
        }
    }

    public string CorFundo => Bipado ? "#dcfce7" : "#f1f5f9";
    public string CorTexto => Bipado ? "#15803d" : "#64748b";
    public string StatusTexto => Bipado ? "LIDO ✓" : "AGUARDAR";
}

public partial class ExpedicaoItensPage : ContentPage
{
    private readonly List<Pedido> _pedidosLote;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private bool _bloqueioFoco = false;
    private int _errosEvitados = 0;
    private readonly ObservableCollection<VolumeExpedicao> _listaVolumes;

    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public ExpedicaoItensPage(Pedido pedido, IPedidoService pedidoService)
        : this(new List<Pedido> { pedido }, pedidoService) { }

    public ExpedicaoItensPage(List<Pedido> pedidosLote, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidosLote = pedidosLote;
        _pedidoService = pedidoService;
        _listaVolumes = new ObservableCollection<VolumeExpedicao>();

        VolumesCollectionView.ItemsSource = _listaVolumes;
        CarregarDados();
        _ = GerarVolumesDoBancoAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        foreach (var p in _pedidosLote)
        {
            await _pedidoService.BloquearPedidoAsync(p.IdPedido, _usuarioLogado);
        }
        GarantirFocoLeitor();
    }

    private void OnLeitorLoaded(object? sender, EventArgs e)
    {
        GarantirFocoLeitor();
    }

    private void GarantirFocoLeitor()
    {
        if (_bloqueioFoco || _isFinalizando) return;

        Dispatcher.Dispatch(() =>
        {
            if (!_bloqueioFoco && !_isFinalizando)
                LeitorBluetoothEntry.Focus();
        });

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
        {
            if (!_bloqueioFoco && !_isFinalizando)
                LeitorBluetoothEntry.Focus();
        });
    }

    private void OnLeitorUnfocused(object sender, FocusEventArgs e)
    {
        if (!_bloqueioFoco && !_isFinalizando)
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
            {
                if (!_bloqueioFoco && !_isFinalizando)
                    LeitorBluetoothEntry.Focus();
            });
        }
    }

    private void OnTelaTapped(object sender, TappedEventArgs e)
    {
        GarantirFocoLeitor();
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
            ClienteLabel.Text = (_pedidosLote[0].Cliente ?? "").ToUpper();
            ModalLabel.Text = "VÁRIOS MODAIS";
        }
    }

    private async Task GerarVolumesDoBancoAsync()
    {
        _listaVolumes.Clear();

        foreach (var p in _pedidosLote)
        {
            var pedidoAtualizado = await _pedidoService.GetPedidoPorIdAsync(p.IdPedido);

            if (pedidoAtualizado?.VolumesPedidos != null && pedidoAtualizado.VolumesPedidos.Any())
            {
                p.VolumesPedidos = pedidoAtualizado.VolumesPedidos;

                foreach (var vol in pedidoAtualizado.VolumesPedidos)
                {
                    _listaVolumes.Add(new VolumeExpedicao
                    {
                        PedidoId = p.IdPedido,
                        CodigoEsperado = vol.CodigoVolume,
                        ExibicaoNome = $"Pedido #{p.IdPedido} | Vol: {vol.CodigoVolume}",
                        Bipado = vol.BipadoExpedicao
                    });
                }
            }
        }
        AtualizarContadores();
        GarantirFocoLeitor();
    }

    private void OnLeitorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue)) return;

        if (e.NewTextValue.Contains('\r') || e.NewTextValue.Contains('\n'))
        {
            string codigoLimpo = e.NewTextValue.Replace("\r", "").Replace("\n", "").Trim();
            LeitorBluetoothEntry.Text = string.Empty;
            if (!string.IsNullOrWhiteSpace(codigoLimpo))
            {
                _ = ProcessarBipagemVolumeAsync(codigoLimpo);
            }
        }
    }

    private async void OnCodigoScaneadoCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.Text))
        {
            string codigo = entry.Text.Replace("\r", "").Replace("\n", "").Trim();
            entry.Text = string.Empty;
            await ProcessarBipagemVolumeAsync(codigo);
        }
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string codigo = await DisplayPromptAsync("Digitação Manual", "Informe o código do Volume:", "Confirmar", "Cancelar");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                await ProcessarBipagemVolumeAsync(codigo.Trim());
            }
        }
        finally
        {
            _bloqueioFoco = false;
            GarantirFocoLeitor();
        }
    }

    private async Task ProcessarBipagemVolumeAsync(string codigo)
    {
        var volumeEncontrado = _listaVolumes.FirstOrDefault(v => v.CodigoEsperado.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        if (volumeEncontrado != null)
        {
            if (!volumeEncontrado.Bipado)
            {
                volumeEncontrado.Bipado = true;

                var pedidoDono = _pedidosLote.FirstOrDefault(p => p.IdPedido == volumeEncontrado.PedidoId);
                var dto = pedidoDono?.VolumesPedidos?.FirstOrDefault(v => v.CodigoVolume.Equals(codigo, StringComparison.OrdinalIgnoreCase));
                if (dto != null) dto.BipadoExpedicao = true;

                AtualizarContadores();
            }
            else
            {
                _errosEvitados++;
                _bloqueioFoco = true;
                try
                {
                    await DisplayAlert("Aviso", "Este volume já foi lido!", "OK");
                }
                finally
                {
                    _bloqueioFoco = false;
                }
            }
        }
        else
        {
            _errosEvitados++;
            _bloqueioFoco = true;
            try
            {
                await DisplayAlert("Erro", $"Código '{codigo}' não pertence a nenhum pedido deste lote.", "OK");
            }
            finally
            {
                _bloqueioFoco = false;
            }
        }

        GarantirFocoLeitor();
    }

    private void AtualizarContadores()
    {
        int totalVolumes = _listaVolumes.Count;
        int volumesBipados = _listaVolumes.Count(v => v.Bipado);
        int pendente = totalVolumes - volumesBipados;

        VolumesRestantesLabel.Text = volumesBipados.ToString();
        VolumesTotaisLabel.Text = totalVolumes.ToString();
        NumeroVolumeGrandeLabel.Text = Math.Max(0, pendente).ToString();

        if (pendente <= 0 && totalVolumes > 0)
        {
            _ = FinalizarExpedicaoAutomaticaAsync();
        }
        else
        {
            GarantirFocoLeitor();
        }
    }

    private async void OnAguardarClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string? motivo = await JustificativaHelper.SolicitarJustificativaAsync(this, "Colocar em Espera");
            if (string.IsNullOrWhiteSpace(motivo)) return;

            bool confirm = await DisplayAlert(
                "Colocar em Espera",
                $"Motivo: {motivo}\nDeseja pausar esta expedição e manter os volumes já bipados salvos?",
                "Sim, Aguardar",
                "Não");

            if (confirm)
            {
                int errosParaEnviar = _errosEvitados;
                foreach (var p in _pedidosLote)
                {
                    if (p.VolumesPedidos != null)
                    {
                        foreach (var volDto in p.VolumesPedidos)
                        {
                            var volLocal = _listaVolumes.FirstOrDefault(v => v.PedidoId == p.IdPedido && v.CodigoEsperado == volDto.CodigoVolume);
                            if (volLocal != null)
                                volDto.BipadoExpedicao = volLocal.Bipado;
                        }
                    }

                    await _pedidoService.PausarPedidoAsync(p, "Expedição", _usuarioLogado, errosParaEnviar, motivo);
                    errosParaEnviar = 0;
                }

                await Navigation.PopAsync();
                return;
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }
        GarantirFocoLeitor();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string? motivo = await JustificativaHelper.SolicitarJustificativaAsync(this, "Cancelar Progresso");
            if (string.IsNullOrWhiteSpace(motivo)) return;

            bool confirm = await DisplayAlert(
                "Atenção",
                $"Motivo: {motivo}\nDeseja sair da expedição e cancelar o progresso atual?",
                "Sim",
                "Não");

            if (confirm)
            {
                foreach (var p in _pedidosLote)
                {
                    await _pedidoService.ReverterEtapaAsync(p.IdPedido, motivo, _usuarioLogado);
                }
                await Navigation.PopAsync();
                return;
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }
        GarantirFocoLeitor();
    }

    private async Task FinalizarExpedicaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        bool sucessoTotal = true;
        string mensagemErro = "";

        int errosParaEnviar = _errosEvitados;

        foreach (var p in _pedidosLote)
        {
            var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(p.IdPedido, p.Status, _usuarioLogado, errosParaEnviar);
            errosParaEnviar = 0;

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
            GarantirFocoLeitor();
        }
    }
}