using Microsoft.Extensions.Logging;
using SpringApp.Services;

namespace SpringApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()// inicializa a aplicação MAUI, configurando serviços, fontes e dependências necessárias para o funcionamento do aplicativo.
        {
            var builder = MauiApp.CreateBuilder(); // Cria uma instância do builder para configurar a aplicação MAUI, permitindo adicionar serviços, fontes e outras configurações antes de construir o aplicativo final.
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); // Adiciona a fonte OpenSans-Regular.ttf com o alias "OpenSansRegular"
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");// Adiciona a fonte OpenSans-Semibold.ttf com o alias "OpenSansSemibold"
                });

            
            builder.Services.AddSingleton(sp => new HttpClient // Adiciona uma instância singleton do HttpClient ao contêiner de serviços, permitindo que a mesma instância seja compartilhada em todo o aplicativo, evitando a criação de múltiplas instâncias e melhorando o desempenho.
            {
                BaseAddress = new Uri("https://localhost:44379") //Indica a URL base da API que será consumida pelo aplicativo, permitindo que todas as solicitações HTTP feitas pelo HttpClient usem essa URL como ponto de partida.
            });

            builder.Services.AddSingleton<ApiServices>();

            return builder.Build();
        }
    }
}
