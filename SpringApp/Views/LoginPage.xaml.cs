using Microsoft.Maui.Storage;

namespace SpringApp.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string textoOriginal = btn.Text;
            btn.Text = "Carregando...";
            btn.IsEnabled = false;
            btn.Opacity = 0.7;

            try
            {
                // Simula o tempo de processamento da API para o efeito visual ser percebido
                await Task.Delay(1000);

                string matricula = MatriculaEntry.Text?.Trim();
                string senha = SenhaEntry.Text?.Trim();

                if ((matricula == "1" && senha == "1") ||
                    (matricula == "2" && senha == "2") ||
                    (matricula == "3" && senha == "3") ||
                    (matricula == "4" && senha == "4"))
                {
                    Preferences.Set("MatriculaLogada", matricula);
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    await DisplayAlert("Atenção", "Matrícula ou senha incorretos!", "OK");
                }
            }
            finally
            {
                btn.Text = textoOriginal;
                btn.IsEnabled = true;
                btn.Opacity = 1.0;
            }
        }
    }
}