using Microsoft.Maui.ApplicationModel.DataTransfer;
using SpringApp.Services;

namespace SpringApp.Views;

public partial class GestaoSupervisorPage : ContentPage
{
    private readonly IPedidoService _pedidoService;

    // O serviço agora é injetado automaticamente
    public GestaoSupervisorPage(IPedidoService pedidoService)
    {
        InitializeComponent();
        _pedidoService = pedidoService;

        string perfilLogado = Preferences.Get("MatriculaLogada", "");
        if (perfilLogado == "3")
        {
            // Restrições de perfil, se necessário
        }
    }

    private async void OnExportarRelatorioClicked(object sender, EventArgs e)
    {
        string operadorSelecionado = FuncionarioPicker.SelectedItem?.ToString() ?? "Todos";

        bool resposta = await DisplayAlert("Exportar Relatório", $"Deseja gerar o relatório de produtividade para: {operadorSelecionado}?", "Sim", "Cancelar");
        if (!resposta) return;

        // Chama o método da interface (seja Mock ou Real)
        string? caminhoArquivo = await _pedidoService.BaixarRelatorioProdutividadeAsync(operadorSelecionado);

        if (!string.IsNullOrEmpty(caminhoArquivo) && File.Exists(caminhoArquivo))
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Relatório de Produtividade Logística",
                File = new ShareFile(caminhoArquivo)
            });
        }
        else
        {
            await DisplayAlert("Erro", "Não foi possível gerar o ficheiro de relatório.", "OK");
        }
    }
}