namespace SpringApp.Views;

public partial class PerfilPage : ContentPage
{
    public PerfilPage()
    {
        InitializeComponent();
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("MatriculaLogada");
        Application.Current.MainPage = new Views.LoginPage();
    }
}