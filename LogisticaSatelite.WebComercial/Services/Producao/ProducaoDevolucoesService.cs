using System.Net.Http.Json;
using System.Text.Json;
using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Producao;

public class ProducaoDevolucoesService : IDevolucoesService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProducaoDevolucoesService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(bool Sucesso, PedidoClienteDevolucaoDto? Dados, string? Erro)> BuscarPedidoClienteAsync(int pedidoId)
    {
        try
        {
            var resp = await _http.GetAsync($"api/devolucoes/pedido-cliente/{pedidoId}");
            if (resp.IsSuccessStatusCode)
            {
                var dados = await resp.Content.ReadFromJsonAsync<PedidoClienteDevolucaoDto>(_jsonOptions);
                return (true, dados, null);
            }

            string erro = await ExtrairMensagemErroAsync(resp);
            return (false, null, erro);
        }
        catch (Exception ex)
        {
            return (false, null, $"Falha de conexão com a API: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, DevolucaoDto? Devolucao, string? Erro)> SolicitarDevolucaoAsync(CriarDevolucaoRequestDto request)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/devolucoes", request);
            if (resp.IsSuccessStatusCode)
            {
                var dev = await resp.Content.ReadFromJsonAsync<DevolucaoDto>(_jsonOptions);
                return (true, dev, null);
            }

            string erro = await ExtrairMensagemErroAsync(resp);
            return (false, null, erro);
        }
        catch (Exception ex)
        {
            return (false, null, $"Falha de conexão com a API: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, List<DevolucaoDto> Lista, string? Erro)> ListarDevolucoesAsync()
    {
        try
        {
            var lista = await _http.GetFromJsonAsync<List<DevolucaoDto>>("api/devolucoes", _jsonOptions) ?? new();
            return (true, lista, null);
        }
        catch (Exception ex)
        {
            return (false, new(), $"Falha ao carregar devoluções: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, DevolucaoDto? Devolucao, string? Erro)> AnalisarDevolucaoAsync(int id, bool aprovada, string usuario, string? motivoRejeicao = null)
    {
        try
        {
            var payload = new
            {
                Aprovada = aprovada,
                Usuario = usuario,
                MotivoRejeicao = motivoRejeicao
            };

            var resp = await _http.PatchAsJsonAsync($"api/devolucoes/{id}/analisar", payload);
            if (resp.IsSuccessStatusCode)
            {
                var dev = await resp.Content.ReadFromJsonAsync<DevolucaoDto>(_jsonOptions);
                return (true, dev, null);
            }

            string erro = await ExtrairMensagemErroAsync(resp);
            return (false, null, erro);
        }
        catch (Exception ex)
        {
            return (false, null, $"Falha de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, List<ProdutoAvariadoDto> Lista, string? Erro)> ListarAvariadosAsync()
    {
        try
        {
            var lista = await _http.GetFromJsonAsync<List<ProdutoAvariadoDto>>("api/avariados", _jsonOptions) ?? new();
            return (true, lista, null);
        }
        catch (Exception ex)
        {
            return (false, new(), $"Falha ao carregar avariados: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string? Erro)> BaixarAvariadoAsync(int id)
    {
        try
        {
            var resp = await _http.DeleteAsync($"api/avariados/{id}");
            if (resp.IsSuccessStatusCode) return (true, null);

            string erro = await ExtrairMensagemErroAsync(resp);
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Falha de conexão: {ex.Message}");
        }
    }

    private static async Task<string> ExtrairMensagemErroAsync(HttpResponseMessage resp)
    {
        try
        {
            var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (json != null && json.TryGetValue("message", out var msg))
                return msg?.ToString() ?? $"Erro HTTP {(int)resp.StatusCode}";
        }
        catch { }
        return $"Erro HTTP {(int)resp.StatusCode}";
    }
}