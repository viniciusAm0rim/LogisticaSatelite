using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

// Classe modelo para os espaços em branco que aguardam o bipe
public class VolumeConferencia
{
    public int Indice { get; set; }
    public string CodigoLido { get; set; }
    public bool Bipado => !string.IsNullOrEmpty(CodigoLido);

    // Se não bipou, pede a caixa. Se bipou, mostra o código colado.
    public string ExibicaoNome => Bipado ? $"Caixa {Indice}: {CodigoLido}" : $"Aguardando bipe da Caixa {Indice}...";
    public string CorFundo => Bipado ? "#dcfce7" : "#f1f5f9";
    public string CorTexto => Bipado ? "#15803d" : "#64748b";
    public string StatusTexto => Bipado ? "LIDO ✓" : "BIPAR";
}

public partial class ConferenciaItensPage : ContentPage
{
    private readonly Pedido _pedido;
    private readonly MockPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    private bool _etapaVolumesAtiva = false;
    private int _qtdVolumesInformada = 0;
    private List<VolumeConferencia> _listaVolumesConferencia = new();

    public ConferenciaItensPage(Pedido pedido)
    {
        InitializeComponent();
        _pedido = pedido;
        _pedidoService = new MockPedidoService();
        CarregarDados();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _pedidoService.BloquearPedidoAsync(_pedido.Id, _usuarioLogado);
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    private void CarregarDados()
    {
        NumeroPedidoLabel.Text = $"#{_pedido.Id:D4}";
        ClienteLabel.Text = (_pedido.Destinatario ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();
        AtualizarContadoresItens();
    }

    private void OnCodigoScaneadoCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.Text))
        {
            string codigo = entry.Text.Trim();
            entry.Text = string.Empty;

            if (!_etapaVolumesAtiva)
            {
                var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo, StringComparison.OrdinalIgnoreCase));
                if (item != null) ProcessarBipagemItem(item);
                else DisplayAlert("Atenção", "Código de produto inválido.", "OK");
            }
            else
            {
                ProcessarBipagemVolume(codigo);
            }

            Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
        }
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        string msg = !_etapaVolumesAtiva ? "Informe o SKU do produto:" : "Informe o código da etiqueta colada no Volume:";
        string codigo = await DisplayPromptAsync("Manual", msg, "Confirmar", "Cancelar");

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            if (!_etapaVolumesAtiva)
            {
                var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));
                if (item != null) ProcessarBipagemItem(item);
                else await DisplayAlert("Erro", "Produto não encontrado.", "OK");
            }
            else
            {
                ProcessarBipagemVolume(codigo.Trim());
            }
        }
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    private void ProcessarBipagemItem(ItemPedido item)
    {
        if (item.BipagensConferencia < item.Quantidade)
        {
            item.BipagensConferencia++;
            AtualizarContadoresItens();
        }
    }

    private void AtualizarContadoresItens()
    {
        int totalItensGeral = _pedido.Itens.Sum(i => i.Quantidade);
        int totalConferidosGeral = _pedido.Itens.Sum(i => i.BipagensConferencia);
        int totalPendente = _pedido.Itens.Sum(i => i.ConferenciaPendente);

        TotalRestanteLabel.Text = totalConferidosGeral.ToString();
        TotalGeralLabel.Text = totalItensGeral.ToString();
        ItensCollectionView.ItemsSource = null;
        ItensCollectionView.ItemsSource = _pedido.Itens;

        if (totalPendente <= 0 && _pedido.Itens.Any()) _ = IniciarEtapaVolumesAsync();
    }

    private async Task IniciarEtapaVolumesAsync()
    {
        if (_etapaVolumesAtiva) return;

        string resposta = await DisplayPromptAsync("Produtos Conferidos!", "Quantos volumes (caixas) este pedido gerou?", "Confirmar", "Cancelar", keyboard: Keyboard.Numeric);

        if (int.TryParse(resposta, out int volumes) && volumes > 0)
        {
            _qtdVolumesInformada = volumes;
            _etapaVolumesAtiva = true;

            // Cria slots vazios aguardando a leitura das etiquetas reais
            _listaVolumesConferencia.Clear();
            for (int i = 1; i <= volumes; i++)
            {
                _listaVolumesConferencia.Add(new VolumeConferencia { Indice = i, CodigoLido = null });
            }

            TituloPaginaLabel.Text = "BIPAR CAIXAS (ETIQUETAS)";
            ContainerItens.IsVisible = false;
            ContainerVolumes.IsVisible = true;
            AtualizarContadoresVolumes();
        }
        else
        {
            await DisplayAlert("Atenção", "Informe um número válido de volumes para prosseguir.", "OK");
        }
    }

    private void ProcessarBipagemVolume(string codigo)
    {
        // Impede que o operador bipe a mesma etiqueta duas vezes sem querer
        if (_listaVolumesConferencia.Any(v => v.CodigoLido != null && v.CodigoLido.Equals(codigo, StringComparison.OrdinalIgnoreCase)))
        {
            DisplayAlert("Aviso", "Esta etiqueta já foi bipada!", "OK");
            return;
        }

        // Acha o primeiro espaço que ainda está vazio
        var slot = _listaVolumesConferencia.FirstOrDefault(v => !v.Bipado);
        if (slot != null)
        {
            slot.CodigoLido = codigo;
            AtualizarContadoresVolumes();
        }
        else
        {
            DisplayAlert("Aviso", "Todos os volumes informados já foram lidos.", "OK");
        }
    }

    private void AtualizarContadoresVolumes()
    {
        int lidos = _listaVolumesConferencia.Count(v => v.Bipado);
        int total = _listaVolumesConferencia.Count;

        VolumesLidosLabel.Text = lidos.ToString();
        VolumesTotalLabel.Text = total.ToString();

        VolumesCollectionView.ItemsSource = null;
        VolumesCollectionView.ItemsSource = _listaVolumesConferencia;

        if (lidos == total && total > 0)
        {
            _ = FinalizarConferenciaNoBancoAsync();
        }
    }

    private async Task FinalizarConferenciaNoBancoAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        // Extrai a lista APENAS com os códigos das etiquetas para enviar ao banco
        var codigos = _listaVolumesConferencia.Select(v => v.CodigoLido).ToList();

        var (sucesso, msg) = await _pedidoService.ConcluirConferenciaAsync(_pedido.Id, codigos, _usuarioLogado);
        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Conferência concluída e caixas identificadas no sistema!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", msg, "OK");
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Atenção", "Deseja sair?", "Sim", "Não");
        if (confirm)
        {
            await _pedidoService.ReverterEtapaAsync(_pedido.Id);
            await Navigation.PopAsync();
        }
    }
}