using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class PedidoService : IPedidoService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

#if ANDROID
    private const string BaseHost = "https://10.0.2.2:44379";
#else
    private const string BaseHost = "https://localhost:44379";
#endif

    public PedidoService()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true };
        _httpClient = new HttpClient(handler);
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/lock", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);
            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        try { return await _httpClient.GetFromJsonAsync<List<Pedido>>($"{BaseHost}/api/pedidos", _jsonOptions) ?? new List<Pedido>(); }
        catch (Exception ex) { Debug.WriteLine($"[ERRO GET ALL]: {ex.Message}"); return new List<Pedido>(); }
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            var pedidos = await GetPedidosAsync();
            return pedidos.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex) { Debug.WriteLine($"[ERRO GET STATUS]: {ex.Message}"); return new List<Pedido>(); }
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseHost}/api/pedidos/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Pedido>(content, _jsonOptions);
            }
        }
        catch (Exception ex) { Debug.WriteLine($"[ERRO GET POR ID]: {ex.Message}"); }
        return null;
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string statusAtual, string usuario = null, int errosEvitados = 0)
    {
        string rota = statusAtual switch
        {
            "Aguardando" => $"api/pedidos/{id}/separar",
            "Em Separaçao" => $"api/pedidos/{id}/AguardandoConfe",
            "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
            "Em Conferencia" => $"api/pedidos/{id}/AguardandoExped",
            "Aguardando Expediçao" => $"api/pedidos/{id}/Expedido",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(rota)) return (false, $"Status '{statusAtual}' não possui rota configurada.");
        try
        {
            var payload = new { Usuario = usuario, ErrosEvitados = errosEvitados };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/{rota}", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);
            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0)
    {
        try
        {
            var payload = new { Volumes = volumes, Usuario = usuario, ErrosEvitados = errosEvitados };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/ConcluirConferencia", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);
            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"{BaseHost}/api/pedidos/{id}/reverter", null);
            if (response.IsSuccessStatusCode) return (true, "");
            return (false, "Erro ao reverter status");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string usuarioFiltro)
    {
        try
        {
            string url = $"{BaseHost}/api/relatorio/exportar-csv?operador={Uri.EscapeDataString(usuarioFiltro)}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                string caminhoFicheiro = Path.Combine(FileSystem.CacheDirectory, "Relatorio_Produtividade.csv");
                File.WriteAllBytes(caminhoFicheiro, bytes);
                return caminhoFicheiro;
            }
            return null;
        }
        catch (Exception) { return null; }
    }
}