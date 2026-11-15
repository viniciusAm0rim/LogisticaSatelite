using SpringApp.Models;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class ConferenciaItensPage : ContentPage
{
    private readonly Pedido _pedido;
    private readonly IPedidoService _pedidoService;
    private bool _isFinalizando = false;
    private bool _bloqueioFoco = false;
    private int _errosEvitados = 0;
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
        NumeroPedidoLabel.Text = $"#{_pedido.IdPedido:D4}";
        ClienteLabel.Text = (_pedido.Cliente ?? "").ToUpper();
        ModalLabel.Text = string.IsNullOrEmpty(_pedido.Modal) ? "PADRÃO" : _pedido.Modal.ToUpper();

        ItensCollectionView.ItemsSource = _pedido.Itens;
        AtualizarContadoresItens();
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
        var item = _pedido.Itens.FirstOrDefault(i => i.CodItem.Equals(codigo, StringComparison.OrdinalIgnoreCase));
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
                await DisplayAlert("Atenção", "Código de produto inválido.", "OK");
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
                    _errosEvitados++;
                    await DisplayAlert("Erro", "Produto não encontrado.", "OK");
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

        if (totalPendente <= 0 && _pedido.Itens.Any())
        {
            _ = IniciarEtapaVolumesAsync();
        }
        else
        {
            GarantirFocoLeitor();
        }
    }

    private async Task IniciarEtapaVolumesAsync()
    {
        int volumes = 0;
        bool volumeValido = false;

        _bloqueioFoco = true;
        try
        {
            while (!volumeValido)
            {
                string resposta = await DisplayPromptAsync("Produtos Conferidos!", "Quantos volumes (caixas) este pedido gerou?", "Confirmar", "Cancelar", keyboard: Keyboard.Numeric);

                if (int.TryParse(resposta, out volumes) && volumes > 0)
                {
                    volumeValido = true;
                }
                else
                {
                    await DisplayAlert("Atenção", "É obrigatório informar um número válido de volumes para prosseguir.", "OK");
                }
            }
        }
        finally
        {
            _bloqueioFoco = false;
        }

        await FinalizarConferenciaNoBancoAsync(volumes);
    }

    private async Task FinalizarConferenciaNoBancoAsync(int qtdVolumes)
    {
        if (_isFinalizando) return;
        _isFinalizando = true;

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
            GarantirFocoLeitor();
        }
    }

    private async void OnAguardarClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            bool confirm = await DisplayAlert("Colocar em Espera", "Deseja pausar esta conferência e manter os itens já bipados salvos?", "Sim, Aguardar", "Não");
            if (confirm)
            {
                var (sucesso, msg) = await _pedidoService.PausarPedidoAsync(_pedido, "Conferência", _usuarioLogado, _errosEvitados);
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

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        _bloqueioFoco = true;
        try
        {
            bool confirm = await DisplayAlert("Atenção", "Deseja sair e cancelar o progresso atual?", "Sim", "Não");
            if (confirm)
            {
                await _pedidoService.ReverterEtapaAsync(_pedido.IdPedido);
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
}