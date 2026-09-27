using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class SeparacaoItensPage : ContentPage
{
    private Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private readonly int _pedidoId;
    private int _errosEvitados = 0; // <-- Variável para acumular os erros da separação
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "NÃO IDENTIFICADO");

    public SeparacaoItensPage(Pedido pedido, IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;
        _pedido = pedido;
        _pedidoId = pedido?.IdPedido ?? 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_pedidoId == 0) return;

        await _pedidoService.BloquearPedidoAsync(_pedidoId, _usuarioLogado);
        await CarregarPedidoDaApiAsync();
        Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
    }

    protected override bool OnBackButtonPressed() => true;

    private async Task CarregarPedidoDaApiAsync()
    {
        try
        {
            var p = await _pedidoService.GetPedidoPorIdAsync(_pedidoId);
            if (p != null) _pedido = p;
        }
        catch { }

        NumeroPedidoLabel.Text = $"#{_pedido.IdPedido:D4}";
        ClienteLabel.Text = (_pedido.Cliente ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();
        AtualizarContadores();
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
                _errosEvitados++; // <-- Contabiliza erro de leitura incorreta na separação
                DisplayAlert("Atenção", "Código inválido.", "OK");
            }
            Device.BeginInvokeOnMainThread(() => { LeitorBluetoothEntry.Focus(); });
        }
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        string codigo = await DisplayPromptAsync("Manual", "Informe o SKU:", "Confirmar", "Cancelar");
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
        if (item.BipagensSeparacao < item.Quantidade)
        {
            item.BipagensSeparacao++;
            AtualizarContadores();
        }
    }

    private void AtualizarContadores()
    {
        if (_pedido?.Itens == null) return;
        int total = _pedido.Itens.Sum(i => i.Quantidade);
        int sep = _pedido.Itens.Sum(i => i.BipagensSeparacao);

        TotalRestanteLabel.Text = sep.ToString();
        TotalGeralLabel.Text = total.ToString();
        ItensCollectionView.ItemsSource = null;
        ItensCollectionView.ItemsSource = _pedido.Itens;

        if (total - sep <= 0 && _pedido.Itens.Any()) _ = FinalizarSeparacaoAutomaticaAsync();
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Atenção", "Deseja cancelar?", "Sim", "Não");
        if (confirm)
        {
            var (sucesso, msg) = await _pedidoService.ReverterEtapaAsync(_pedido.IdPedido);
            if (sucesso) await Navigation.PopAsync();
        }
    }

    private async Task FinalizarSeparacaoAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        // Envia os erros evitados acumulados durante a separação para a API
        var (sucesso, msg) = await _pedidoService.AvancarEtapaAsync(_pedido.IdPedido, _pedido.Status, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            await DisplayAlert("Sucesso", "Separação concluída!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", msg, "OK");
        }
    }
}