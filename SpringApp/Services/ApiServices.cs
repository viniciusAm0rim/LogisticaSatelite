using SpringApp.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SpringApp.Services;

public class ApiServices : IPedidoService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiServices(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Pedido>>("api/pedidos", _jsonOptions) ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            string endpoint = string.IsNullOrEmpty(status)
                ? "api/pedidos"
                : $"api/pedidos?status={Uri.EscapeDataString(status)}";
            return await _httpClient.GetFromJsonAsync<List<Pedido>>(endpoint, _jsonOptions) ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    public async Task<List<Pedido>> ObterPorClienteAsync(string termoCliente)
    {
        try
        {
            var todos = await GetPedidosAsync();
            if (string.IsNullOrWhiteSpace(termoCliente)) return todos;
            termoCliente = termoCliente.ToLower().Trim();
            return todos.Where(p =>
                (!string.IsNullOrEmpty(p.Cliente) && p.Cliente.ToLower().Contains(termoCliente)) ||
                p.IdPedido.ToString() == termoCliente
            ).ToList();
        }
        catch (Exception)
        {
            return new();
        }
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Pedido>($"api/pedidos/{id}", _jsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        try
        {
            var dto = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/lock", dto, _jsonOptions);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao bloquear pedido.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string statusAtual, string usuario = null, int errosEvitados = 0)
    {
        string endpoint = statusAtual switch
        {
            "Aguardando" => $"api/pedidos/{id}/separar",
            "Em Separação" => $"api/pedidos/{id}/AguardandoConfe",
            "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
            "Aguardando Expedição" => $"api/pedidos/{id}/Expedido",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(endpoint))
        {
            return (false, "Este pedido já está em um estado final ou endpoint inválido.");
        }

        var dto = new { Usuario = usuario, ErrosEvitados = errosEvitados };
        var response = await _httpClient.PatchAsJsonAsync(endpoint, dto, _jsonOptions);

        if (response.IsSuccessStatusCode)
        {
            return (true, string.Empty);
        }

        if ((int)response.StatusCode == 422)
        {
            var error = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(_jsonOptions);
            return (false, error?.Detail ?? "Violação de regra de negócio.");
        }

        return (false, "Ocorreu um erro inesperado ao processar a requisição.");
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0)
    {
        try
        {
            string endpoint = $"api/pedidos/{id}/ConcluirConferencia";
            var dto = new { Usuario = usuario, Volumes = volumes, ErrosEvitados = errosEvitados };
            var response = await _httpClient.PatchAsJsonAsync(endpoint, dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            return (false, "Erro ao concluir a conferência e gerar volumes.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"api/pedidos/{id}/reverter", null);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao reverter etapa.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        try
        {
            string url = $"api/relatorio/exportar-csv?operador={Uri.EscapeDataString(operadorFiltro)}";
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
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada)
    {
        try
        {
            var payload = new { PedidoId = id, DataHora = dataHoraEstimada, DataHoraEstimada = dataHoraEstimada };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/responder-estimativa", payload, _jsonOptions);
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

public class ProblemDetailsDto
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
}