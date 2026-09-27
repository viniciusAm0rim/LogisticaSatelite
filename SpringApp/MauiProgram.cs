using Microsoft.Extensions.Logging;
using SpringApp.Services;
using SpringApp.Views; // Certifique-se de importar o namespace das suas views

namespace SpringApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:44379")
            });

            // =================================================================
            // CHAVE DE ALTERNÂNCIA ENTRE MOCK E API REAL
            // true  -> Usa o Mock (Testes Locais)
            // false -> Usa a API Real (Banco de Dados)
            // =================================================================
            const bool usarMockLocal = true;

            if (usarMockLocal)
            {
                builder.Services.AddSingleton<IPedidoService, MockPedidoService>();
            }
            else
            {
                builder.Services.AddSingleton<IPedidoService, PedidoService>();
            }

            // Registrar as páginas para que recebam a injeção automaticamente
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<GestaoSupervisorPage>();
            // Se tiver outras páginas que usam o IPedidoService, registre-as aqui também, ex:
            // builder.Services.AddTransient<SeparacaoItensPage>();

            return builder.Build();
        }
    }
}
