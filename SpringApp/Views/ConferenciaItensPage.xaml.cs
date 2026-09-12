using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class ConferenciaItensPage : ContentPage
{
    private readonly Pedido _pedido;
    private readonly PedidoService _pedidoService;
    private bool _isFinalizando = false;

    public ConferenciaItensPage(Pedido pedido)
    {
        InitializeComponent();
        _pedido = pedido;
        _pedidoService = new PedidoService();

        CarregarDados();
    }

    private void CarregarDados()
    {
        NumeroPedidoLabel.Text = $"Pedido #{_pedido.Id} - {_pedido.Numero}";
        ClienteLabel.Text = $"Cliente: {_pedido.Destinatario}";
        SubtituloSeparadorLabel.Text = $"Separador: {_pedido.SepUser ?? "Não informado"}";
        AtualizarContadores();
    }

    private void OnBiparClicked(object sender, EventArgs e) => ExecutarBipagemPorCodigo(CodigoBarraEntry.Text?.Trim());
    private void OnBiparCompleted(object sender, EventArgs e) => ExecutarBipagemPorCodigo(CodigoBarraEntry.Text?.Trim());
    private void OnBiparItemDiretoClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ItemPedido item)
        {
            ProcessarBipagemItem(item);
        }
    }

    private void ExecutarBipagemPorCodigo(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return;

        var item = _pedido.Itens.FirstOrDefault(i =>
            i.Nome.Equals(codigo, StringComparison.OrdinalIgnoreCase) ||
            i.Id.ToString() == codigo);

        if (item != null)
        {
            ProcessarBipagemItem(item);
        }
        else
        {
            DisplayAlert("Atenção", "Item não pertence a este pedido!", "OK");
        }

        CodigoBarraEntry.Text = string.Empty;
        CodigoBarraEntry.Focus();
    }

    private void ProcessarBipagemItem(ItemPedido item)
    {
        if (item.BipagensConferencia < item.Quantidade)
        {
            item.BipagensConferencia++;
            AtualizarContadores();
        }
        else
        {
            DisplayAlert("Aviso", "A quantidade deste item já foi totalmente conferida!", "OK");
        }
    }

    private void AtualizarContadores()
    {
        int totalItensGeral = _pedido.Itens.Sum(i => i.Quantidade);
        int totalConferidosGeral = _pedido.Itens.Sum(i => i.BipagensConferencia);
        int totalPendente = _pedido.Itens.Sum(i => i.ConferenciaPendente);

        TotalRestanteLabel.Text = $"{totalConferidosGeral} de {totalItensGeral}";

        ItensCollectionView.ItemsSource = null;
        ItensCollectionView.ItemsSource = _pedido.Itens;

        if (totalPendente <= 0 && _pedido.Itens.Any())
        {
            _ = TratarConclusaoConferenciaAsync();
        }
    }

    private async Task TratarConclusaoConferenciaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        string resposta = await DisplayPromptAsync(
            "Conferência Finalizada",
            "Informe a quantidade total de volumes deste pedido:",
            accept: "Confirmar",
            cancel: "Cancelar",
            keyboard: Keyboard.Numeric);

        if (int.TryParse(resposta, out int volumes))
        {
            _pedido.Volumes = volumes;
            _pedido.Status = "Aguardando Expediçao";

            var (sucesso, mensagem) = await _pedidoService.ConcluirConferenciaAsync(_pedido.Id, volumes);

            if (sucesso)
            {
                await DisplayAlert("Sucesso", "Conferência concluída! Pedido enviado para Aguardando Expediçao.", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                _isFinalizando = false;
                await DisplayAlert("Erro ao Concluir", mensagem, "OK");
            }
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Atenção", "É obrigatório informar um número válido de volumes para finalizar.", "OK");
        }
    }
}