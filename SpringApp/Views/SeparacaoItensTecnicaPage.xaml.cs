using SpringApp.Helpers;
using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class SeparacaoItensTecnicaPage : ContentPage
{
    private Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private bool _bloqueioFoco = false;
    private readonly int _pedidoId;
    private int _errosEvitados = 0;
    private List<ItemPedido> _itensTecnicos = new();
    private string _usuarioLogado => Preferences.Get("UsuarioNome", "Área Técnica");

    public SeparacaoItensTecnicaPage(Pedido pedido, IPedidoService pedidoService)
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

        await CarregarPedidoDaApiAsync();
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
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "ROTA TÉCNICA" : _pedido.Modal.ToUpper();

        // Filtra apenas os itens com Rota = 1 (Area Tecnica) definida no banco
        _itensTecnicos = (_pedido.Itens ?? new List<ItemPedido>())
            .Where(i => i.TipoRota == 1)
            .ToList();

        ItensCollectionView.ItemsSource = _itensTecnicos;
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
                _ = ValidarCodigoLidoAsync(codigoLimpo);
            }
        }
    }

    private async void OnCodigoScaneadoCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.Text))
        {
            string codigo = entry.Text.Replace("\r", "").Replace("\n", "").Trim();
            entry.Text = string.Empty;
            await ValidarCodigoLidoAsync(codigo);
        }
    }

    private async Task ValidarCodigoLidoAsync(string codigo)
    {
        var item = _itensTecnicos.FirstOrDefault(i => i.CodItem.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            ProcessarBipagemItem(item);
        }
        else
        {
            _errosEvitados++;
            _bloqueioFoco = true;
            try
            {
                await DisplayAlert("Atenção", "Código inválido ou não pertence à Área Técnica.", "OK");
            }
            finally
            {
                _bloqueioFoco = false;
            }
        }

        GarantirFocoLeitor();
    }

    private async void OnDigitarCodigoManualClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            string codigo = await DisplayPromptAsync("Etiqueta Técnica", "Informe o código do item técnico após teste/produção:", "Confirmar", "Cancelar");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var item = _itensTecnicos.FirstOrDefault(i => i.CodItem.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    ProcessarBipagemItem(item);
                }
                else
                {
                    _errosEvitados++;
                    await DisplayAlert("Erro", "Produto técnico não encontrado neste pedido.", "OK");
                }
            }
        }
        finally
        {
            _bloqueioFoco = false;
            GarantirFocoLeitor();
        }
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
        int total = _itensTecnicos.Sum(i => i.Quantidade);
        int sep = _itensTecnicos.Sum(i => i.BipagensSeparacao);

        TotalRestanteLabel.Text = sep.ToString();
        TotalGeralLabel.Text = total.ToString();

        if (total - sep <= 0 && _itensTecnicos.Any())
        {
            _ = FinalizarSeparacaoTecnicaAutomaticaAsync();
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
            string? motivo = await JustificativaHelper.SolicitarJustificativaAsync(this, "Colocar em Espera (Área Técnica)");
            if (string.IsNullOrWhiteSpace(motivo)) return;

            bool confirm = await DisplayAlert(
                "Colocar em Espera",
                $"Motivo: {motivo}\nDeseja pausar a etapa técnica?",
                "Sim, Aguardar",
                "Não");

            if (confirm)
            {
                var (sucesso, msg) = await _pedidoService.PausarPedidoAsync(_pedido, "Área Técnica", _usuarioLogado, _errosEvitados, motivo);
                if (sucesso)
                {
                    await Navigation.PopAsync();
                    return;
                }
                await DisplayAlert("Erro", msg, "OK");
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }
        GarantirFocoLeitor();
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async Task FinalizarSeparacaoTecnicaAutomaticaAsync()
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

        var (sucesso, msg) = await _pedidoService.ConcluirSeparacaoRotaAsync(_pedido, 1, _usuarioLogado, _errosEvitados);
        if (sucesso)
        {
            var pedidoAtualizado = await _pedidoService.GetPedidoPorIdAsync(_pedidoId);
            bool liberadoConferencia = pedidoAtualizado != null &&
                                       pedidoAtualizado.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase);

            string textoSucesso = liberadoConferencia
                ? "Item técnico concluído! Todos os itens foram bipados e o pedido foi liberado para Conferência."
                : "Item técnico concluído! O pedido aguarda o Separador finalizar os itens comuns para liberar a Conferência.";

            await DisplayAlert("Área Técnica Concluída", textoSucesso, "OK");
            await Navigation.PopAsync();
        }
        else
        {
            _isFinalizando = false;
            await DisplayAlert("Erro", msg, "OK");
            GarantirFocoLeitor();
        }
    }
}