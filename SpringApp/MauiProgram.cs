using Microsoft.Extensions.Logging;
using SpringApp.Services;
using SpringApp.Views;

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

            // IP e Porta HTTP testados e validados no navegador
            string ipComputador = "192.168.15.12";
            string baseAddress = $"http://{ipComputador}:5283";

            var handler = new HttpClientHandler
            {
                // Mantém a flexibilidade para chamadas HTTP/HTTPS na rede local
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            builder.Services.AddSingleton(sp => new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress)
            });

            // =================================================================
            // CHAVE DE ALTERNÂNCIA ENTRE MOCK E API REAL
            // =================================================================
            const bool usarMockLocal = false;
            if (usarMockLocal)
            {
                builder.Services.AddSingleton<IPedidoService, MockPedidoService>();
            }
            else
            {
                builder.Services.AddSingleton<IPedidoService, ApiServices>();
            }

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<GestaoSupervisorPage>();

            return builder.Build();
        }
    }
}