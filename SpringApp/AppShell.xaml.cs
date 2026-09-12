using Microsoft.Maui.Storage;

namespace SpringApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        VerificarSessao();
    }

    private void VerificarSessao()
    {
        string matricula = Preferences.Get("MatriculaLogada", string.Empty);

        // Se por algum motivo o usuário entrar sem estar logado, redireciona para o Login
        if (string.IsNullOrEmpty(matricula))
        {
            Current.GoToAsync("//LoginPage");
        }
    }
}