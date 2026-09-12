using SpringApp.Views;

namespace SpringApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Inicializa o app na tela de login dentro de uma janela com tamanho de coletor (400x800)
        var window = new Window(new LoginPage())
        {
            Width = 400,
            Height = 700
        };

        return window;
    }
}