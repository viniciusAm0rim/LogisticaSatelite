using System.Net.Http.Json;
using System.Text.Json;
using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Producao;

public class ProducaoPedidosService : IPedidosService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProducaoPedidosService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProducaoPedidosService(HttpClient httpClient, ILogger<ProducaoPedidosService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ResultadoConsultaPedidos> ObterPedidosAsync()
    {
        try
        {
            var pedidos = await _httpClient.GetFromJsonAsync<List<PedidoDto>>("api/Pedidos", _jsonOptions);
            return new ResultadoConsultaPedidos(true, pedidos ?? new List<PedidoDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar pedidos na API de produção.");
            return new ResultadoConsultaPedidos(
                false,
                new List<PedidoDto>(),
                "Não foi possível conectar ao servidor da API. Verifique se o serviço está em execução na rede."
            );
        }
    }

    public async Task<bool> SolicitarEstimativaAsync(int pedidoId)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"api/Pedidos/{pedidoId}/solicitar-estimativa", null);

            if (!response.IsSuccessStatusCode)
            {
                response = await _httpClient.PostAsync($"api/Pedidos/{pedidoId}/solicitar-estimativa", null);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar estimativa para o pedido #{PedidoId}.", pedidoId);
            return false;
        }
    }
}