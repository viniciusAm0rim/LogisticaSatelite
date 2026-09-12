using System.Diagnostics;
using System.Net.Http.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class PedidoService
{
    private readonly HttpClient _httpClient;
#if ANDROID
    private const string BaseHost = "https://10.0.2.2:44379";
#else
    private const string BaseHost = "https://localhost:44379";
#endif

    public PedidoService()
    {
        // Ignora erros de certificado SSL no ambiente local de desenvolvimento
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            var pedidos = await _httpClient.GetFromJsonAsync<List<Pedido>>($"{BaseHost}/api/pedidos")
                          ?? new List<Pedido>();

            // Filtro local pois a API atual retorna a lista completa sem filtrar por parâmetro GET
            return pedidos.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERRO GET]: {ex.Message}");
            return new List<Pedido>();
        }
    }

    public Task<List<Pedido>> ObterPedidosPorStatusAsync(string status)
        => ObterPorStatusAsync(status);

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string statusAtual)
    {
        // Mapeamento idêntico às rotas do seu PedidosController
        string rota = statusAtual switch
        {
            "Aguardando" => $"api/pedidos/{id}/separar",
            "Em Separaçao" => $"api/pedidos/{id}/AguardandoConfe",
            "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
            "Em Conferencia" => $"api/pedidos/{id}/AguardandoExped",
            "Aguardando Expediçao" => $"api/pedidos/{id}/Expedido",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(rota))
            return (false, $"Status '{statusAtual}' não possui rota configurada.");

        try
        {
            var response = await _httpClient.PatchAsync($"{BaseHost}/{rota}", null);

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes)
    {
        try
        {
            // Envia o total de volumes informados para a rota PATCH correspondente na API
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/ConcluirConferencia", volumes);

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }
}