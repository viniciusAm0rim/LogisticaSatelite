using System.Net.Http.Json;
using System.Text.Json;
using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Producao;

public class ProducaoCotacoesService : ICotacoesService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProducaoCotacoesService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProducaoCotacoesService(HttpClient httpClient, ILogger<ProducaoCotacoesService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<CotacaoDto>> ObterCotacoesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CotacaoDto>>("api/Cotacoes", _jsonOptions)
                   ?? new List<CotacaoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter cotações da API.");
            return new List<CotacaoDto>();
        }
    }

    public async Task<List<ProdutoCatalogoDto>> BuscarProdutosAsync(string termo)
    {
        try
        {
            var url = $"api/Cotacoes/produtos?termo={Uri.EscapeDataString(termo ?? string.Empty)}";
            return await _httpClient.GetFromJsonAsync<List<ProdutoCatalogoDto>>(url, _jsonOptions)
                   ?? new List<ProdutoCatalogoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produtos para cotação.");
            return new List<ProdutoCatalogoDto>();
        }
    }

    public async Task<(bool Sucesso, string? MensagemErro)> CriarCotacaoAsync(NovaCotacaoRequestDto novaCotacao)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/Cotacoes", novaCotacao);
            if (response.IsSuccessStatusCode)
                return (true, null);

            var detalhe = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(detalhe) ? "Falha ao registrar cotação." : detalhe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar solicitação de cotação.");
            return (false, "Não foi possível conectar ao servidor da API.");
        }
    }
}
