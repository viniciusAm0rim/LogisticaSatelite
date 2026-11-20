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
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(6)
        };
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        return await _httpClient.GetFromJsonAsync<List<Pedido>>($"{BaseHost}/api/pedidos", _jsonOptions, cts.Token) ?? new List<Pedido>();
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            var pedidos = await GetPedidosAsync();
            return pedidos
                .Where(p => !p.EmEspera && string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex) { Debug.WriteLine($"[ERRO GET STATUS]: {ex.Message}"); return new List<Pedido>(); }
    }

    public async Task<List<Pedido>> ObterPorClienteAsync(string termoCliente)
    {
        try
        {
            var pedidos = await GetPedidosAsync();
            if (string.IsNullOrWhiteSpace(termoCliente)) return pedidos;
            termoCliente = termoCliente.ToLower().Trim();
            return pedidos.Where(p =>
                (!string.IsNullOrEmpty(p.Cliente) && p.Cliente.ToLower().Contains(termoCliente)) ||
                p.IdPedido.ToString() == termoCliente
            ).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERRO GET POR CLIENTE]: {ex.Message}");
            return new List<Pedido>();
        }
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var response = await _httpClient.GetAsync($"{BaseHost}/api/pedidos/{id}", cts.Token);
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
            "Em Separação" => $"api/pedidos/{id}/AguardandoConfe",
            "Em Separaçao" => $"api/pedidos/{id}/AguardandoConfe",
            "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
            "Em Conferencia" => $"api/pedidos/{id}/AguardandoExped",
            "Aguardando Expedição" => $"api/pedidos/{id}/Expedido",
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

    public async Task<(bool Sucesso, string Mensagem)> PausarPedidoAsync(Pedido pedido, string etapa, string usuario, int errosEvitados = 0, string? motivo = null)
    {
        try
        {
            var payload = new
            {
                Etapa = etapa,
                Usuario = usuario,
                Motivo = motivo,
                ErrosEvitados = errosEvitados,
                Itens = pedido.Itens?.Select(i => new
                {
                    Id = i.Id,
                    CodItem = i.CodItem,
                    ScansSep = i.BipagensSeparacao,
                    ScansConf = i.BipagensConferencia
                }).ToList(),
                VolumesPedidos = pedido.VolumesPedidos?.Select(v => new
                {
                    Id = v.Id,
                    CodigoVolume = v.CodigoVolume,
                    BipadoExpedicao = v.BipadoExpedicao
                }).ToList()
            };

            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{pedido.IdPedido}/pausar", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> RetomarPedidoAsync(int id, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/retomar", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id, string? motivo = null, string? usuario = null)
    {
        try
        {
            var payload = new { Usuario = usuario, Motivo = motivo };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/reverter", payload);
            if (response.IsSuccessStatusCode) return (true, "");
            return (false, "Erro ao reverter status");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario, string? motivo = null)
    {
        try
        {
            var payload = new { Usuario = usuario, Motivo = motivo };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/cancelar", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
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
                string caminhoArquivo = Path.Combine(FileSystem.CacheDirectory, "Relatorio_Produtividade.csv");
                File.WriteAllBytes(caminhoArquivo, bytes);
                return caminhoArquivo;
            }
            return null;
        }
        catch (Exception) { return null; }
    }

    public async Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada, string etapa = "", string usuario = "")
    {
        try
        {
            var payload = new
            {
                PedidoId = id,
                DataHora = dataHoraEstimada,
                DataHoraEstimada = dataHoraEstimada,
                Etapa = etapa,
                Usuario = usuario
            };

            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/responder-estimativa", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<List<Cotacao>> GetCotacoesPendentesAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            return await _httpClient.GetFromJsonAsync<List<Cotacao>>($"{BaseHost}/api/cotacoes?status=Pendente", _jsonOptions, cts.Token)
                   ?? new List<Cotacao>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERRO GET COTACOES]: {ex.Message}");
            return new List<Cotacao>();
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ResponderCotacaoAsync(int id, int volumes, double pesoTotalKg, string dimensoes, string usuario)
    {
        try
        {
            var payload = new
            {
                Volumes = volumes,
                PesoTotalKg = pesoTotalKg,
                Dimensoes = dimensoes,
                Usuario = usuario
            };

            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/cotacoes/{id}/responder", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }
}