using System.Net.Http.Json;
using System.Text.Json;
using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services;

public class ComercialApiService : IComercialApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ComercialApiService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ComercialApiService(HttpClient httpClient, ILogger<ComercialApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<PedidoDto>> ObterPedidosAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<PedidoDto>>("api/Pedidos", _jsonOptions);
            return result ?? new List<PedidoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter lista de pedidos da API LogisticaSatelite.");
            return new List<PedidoDto>();
        }
    }

    public async Task<PedidoDto?> ObterPedidoPorIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PedidoDto>($"api/Pedidos/{id}", _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar pedido ID {PedidoId} na API.", id);
            return null;
        }
    }

    public async Task<bool> SolicitarEstimativaAsync(int pedidoId)
    {
        try
        {
            // Tenta enviar via PATCH (padrão para atualização parcial de estimativa)
            var response = await _httpClient.PatchAsync($"api/Pedidos/{pedidoId}/solicitar-estimativa", null);

            if (!response.IsSuccessStatusCode)
            {
                // Fallback para POST caso o endpoint aceite POST
                response = await _httpClient.PostAsync($"api/Pedidos/{pedidoId}/solicitar-estimativa", null);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar estimativa para o pedido ID {PedidoId}.", pedidoId);
            return false;
        }
    }
}
