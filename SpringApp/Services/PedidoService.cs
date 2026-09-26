using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class PedidoService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

#if ANDROID
    private const string BaseHost = "https://10.0.2.2:44379";
#else
    private const string BaseHost = "https://localhost:44379";
#endif

   
    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/lock", payload);

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


    public PedidoService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // --- NOVO MÉTODO PARA BUSCAR TODOS OS PEDIDOS (Usado no histórico) ---
    public async Task<List<Pedido>> GetPedidosAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Pedido>>($"{BaseHost}/api/pedidos", _jsonOptions)
                   ?? new List<Pedido>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERRO GET ALL]: {ex.Message}");
            return new List<Pedido>();
        }
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            var pedidos = await GetPedidosAsync();
            return pedidos.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERRO GET STATUS]: {ex.Message}");
            return new List<Pedido>();
        }
    }

    public Task<List<Pedido>> ObterPedidosPorStatusAsync(string status)
        => ObterPorStatusAsync(status);

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseHost}/api/pedidos/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[JSON RECEBIDO DA API]: {content}");
                return JsonSerializer.Deserialize<Pedido>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERRO GET POR ID]: {ex.Message}");
        }
        return null;
    }

    // --- ATUALIZADO PARA ENVIAR O USUÁRIO NO CORPO DA REQUISIÇÃO (JSON) ---
    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string statusAtual, string usuario = null)
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

        if (string.IsNullOrEmpty(rota))
            return (false, $"Status '{statusAtual}' não possui rota configurada.");

        try
        {
            var payload = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/{rota}", payload);

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

    // --- ATUALIZADO PARA ENVIAR VOLUMES E O USUÁRIO ---
    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null)
    {
        try
        {
            var payload = new { Volumes = volumes, Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/ConcluirConferencia", payload);

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

    public async Task<List<Produto>> ObterRecomendacoesAbastecimentoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseHost}/api/Abastecimento/recomendacoes");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Produto>>(content, _jsonOptions) ?? new List<Produto>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERRO ABASTECIMENTO GET]: {ex.Message}");
        }
        return new List<Produto>();
    }

    public async Task<(bool Sucesso, string Mensagem)> ConfirmarAbastecimentoAsync(int produtoId, string matricula)
    {
        try
        {
            var dto = new { ProdutoId = produtoId, MatriculaUsuario = matricula };
            var jsonContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseHost}/api/Abastecimento/confirmar", jsonContent);
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
}