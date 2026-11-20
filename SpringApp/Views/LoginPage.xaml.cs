using SpringApp.Services;

namespace SpringApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;

    public LoginPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    // Construtor sem parâmetros caso seja instanciado via new LoginPage() no logout
    public LoginPage() : this(IPlatformApplication.Current!.Services.GetRequiredService<IAuthService>())
    {
    }

    private async void OnEntrarClicked(object sender, EventArgs e)
    {
        string matricula = MatriculaEntry.Text?.Trim() ?? string.Empty;
        string senha = SenhaEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(matricula) || string.IsNullOrEmpty(senha))
        {
            await DisplayAlert("Atenção", "Por favor, informe o usuário/e-mail e a senha.", "OK");
            return;
        }

        SetLoadingState(true);

        var (sucesso, mensagem) = await _authService.RealizarLoginAsync(matricula, senha);

        if (sucesso)
        {
            string perfilDb = Preferences.Get("UsuarioPerfil", string.Empty).Trim();
            string matriculaMapeada = MapearPerfilParaMatricula(perfilDb);
            Preferences.Set("MatriculaLogada", matriculaMapeada);
        }

        SetLoadingState(false);

        if (sucesso)
        {
            var mainPage = Handler.MauiContext.Services.GetService<MainPage>();
            Application.Current.MainPage = new NavigationPage(mainPage);
        }
        else
        {
            await DisplayAlert("Acesso Negado", string.IsNullOrEmpty(mensagem) ? "Usuário ou senha inválidos." : mensagem, "OK");
        }
    }

    private static string MapearPerfilParaMatricula(string perfilDb)
    {
        return perfilDb.ToLowerInvariant() switch
        {
            "separador" or "1" => "1",
            "conferente" or "2" => "2",
            "hibrido" or "híbrido" or "3" => "3",
            "supervisor" or "gestao" or "gestão" or "4" => "4",
            "area tecnica" or "área técnica" or "tecnica" or "técnica" or "5" => "5",
            _ => "3"
        };
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