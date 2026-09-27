using SpringApp.Services;

namespace SpringApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage()
    {
        InitializeComponent();
        _authService = new AuthService();
    }

    private async void OnEntrarClicked(object sender, EventArgs e)
    {
        string matricula = MatriculaEntry.Text?.Trim() ?? string.Empty;
        string senha = SenhaEntry.Text?.Trim() ?? string.Empty;

        // Validação no frontend antes de bater na API
        if (string.IsNullOrEmpty(matricula) || string.IsNullOrEmpty(senha))
        {
            await DisplayAlert("Atenção", "Por favor, informe a matrícula e a senha.", "OK");
            return;
        }

        SetLoadingState(true);

        bool sucesso = false;
        string mensagem = string.Empty;

        // =========================================================================
        // BYPASS OFFLINE PARA APRESENTAÇÃO (Basta apagar este bloco após o evento)
        // =========================================================================
        if (matricula.Equals("vinicius@teste.com", StringComparison.OrdinalIgnoreCase) && senha == "1234")
        {
            Preferences.Set("UsuarioNome", "Vinicius Ferreira");
            Preferences.Set("MatriculaLogada", "3"); // Perfil Híbrido
            sucesso = true;
        }
        else if (matricula.Equals("supervisor@teste.com", StringComparison.OrdinalIgnoreCase) && senha == "1234")
        {
            Preferences.Set("UsuarioNome", "Gestão/Supervisor");
            Preferences.Set("MatriculaLogada", "4"); // Perfil Supervisor
            sucesso = true;
        }
        else
        {
            // Fluxo normal da API / Banco real
            var resultado = await _authService.RealizarLoginAsync(matricula, senha);
            sucesso = resultado.Sucesso;
            mensagem = resultado.Mensagem;
        }
        // =========================================================================

        SetLoadingState(false);

        if (sucesso)
        {
            string perfilLogado = Preferences.Get("MatriculaLogada", "");

            // CORREÇÃO: Pede ao Injetor de Dependência do MAUI para criar a MainPage já com o serviço certo
            var mainPage = Handler.MauiContext.Services.GetService<MainPage>();

            if (perfilLogado == "4")
            {
                Application.Current.MainPage = new NavigationPage(mainPage);
            }
            else
            {
                Application.Current.MainPage = new NavigationPage(mainPage);
            }
        }
        else
        {
            await DisplayAlert("Acesso Negado", string.IsNullOrEmpty(mensagem) ? "E-mail ou senha inválidos." : mensagem, "OK");
        }
    }

    private async void OnEsqueciSenhaTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Recuperação de Senha", "Entre em contato com o suporte de TI para redefinir sua senha.", "OK");
    }

    private void SetLoadingState(bool isLoading)
    {
        EntrarBtn.Text = isLoading ? string.Empty : "ENTRAR NO SISTEMA";
        EntrarBtn.IsEnabled = !isLoading;
        LoadingSpinner.IsVisible = isLoading;
        LoadingSpinner.IsRunning = isLoading;
    }
}