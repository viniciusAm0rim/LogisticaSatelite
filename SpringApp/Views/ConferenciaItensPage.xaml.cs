using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class ConferenciaItensPage : ContentPage
{
    private readonly Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private int _errosEvitados = 0; // <-- Variável para acumular os erros da conferência
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public ConferenciaItensPage(Pedido pedido, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedido = pedido;
        _pedidoService = pedidoService;
        CarregarDados();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _pedidoService.BloquearPedidoAsync(_pedido.IdPedido, _usuarioLogado);
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    private void CarregarDados()
    {
        NumeroPedidoLabel.Text = $"#{_pedido.IdPedido:D4}";
        ClienteLabel.Text = (_pedido.Cliente ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();
        AtualizarContadoresItens();
    }

    private void OnCodigoScaneadoCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.Text))
        {
            string codigo = entry.Text.Trim();
            entry.Text = string.Empty;

            var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                ProcessarBipagemItem(item);
            }
            else
            {
                _errosEvitados++; // <-- Contabiliza erro de leitura incorreta na conferência
                DisplayAlert("Atenção", "Código de produto inválido.", "OK");
            }

            Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
        }
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        string codigo = await DisplayPromptAsync("Manual", "Informe o SKU do produto:", "Confirmar", "Cancelar");

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                ProcessarBipagemItem(item);
            }
            else
            {
                _errosEvitados++; // <-- Contabiliza erro manual incorreto
                await DisplayAlert("Erro", "Produto não encontrado.", "OK");
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
        string resposta = await DisplayPromptAsync("Produtos Conferidos!", "Quantos volumes (caixas) este pedido gerou?", "Confirmar", "Cancelar", keyboard: Keyboard.Numeric);

        if (int.TryParse(resposta, out int volumes) && volumes > 0)
        {
            await FinalizarConferenciaNoBancoAsync(volumes);
        }
        else
        {
            await DisplayAlert("Atenção", "Informe um número válido de volumes para prosseguir.", "OK");
        }
    }

    private async Task FinalizarConferenciaNoBancoAsync(int qtdVolumes)
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        // Envia a quantidade de volumes e os erros acumulados para a API
        var (sucesso, msg) = await _pedidoService.ConcluirConferenciaAsync(_pedido.IdPedido, qtdVolumes, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Conferência concluída com sucesso!", "OK");
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
            await _pedidoService.ReverterEtapaAsync(_pedido.IdPedido);
            await Navigation.PopAsync();
        }
    }
}