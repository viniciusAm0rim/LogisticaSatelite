using System.Net.Http.Json;
using System.Text.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class PedidoService : IPedidoService
{
    private readonly HttpClient _httpClient;

    private string BaseHost => Preferences.Get("ApiUrl", "https://logisticasatelite-api.onrender.com").TrimEnd('/');

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PedidoService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Pedido>>($"{BaseHost}/api/pedidos", _jsonOptions) ?? new List<Pedido>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro de API: {ex.Message}");
            return new List<Pedido>();
        }
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Pedido>>($"{BaseHost}/api/pedidos?status={Uri.EscapeDataString(status)}", _jsonOptions) ?? new List<Pedido>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro de API: {ex.Message}");
            return new List<Pedido>();
        }
    }

    public async Task<List<Pedido>> ObterPorClienteAsync(string termoCliente)
    {
        var todos = await GetPedidosAsync();
        if (string.IsNullOrWhiteSpace(termoCliente)) return todos;
        return todos.Where(p => p.Cliente != null && p.Cliente.Contains(termoCliente, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Pedido>($"{BaseHost}/api/pedidos/{id}", _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/lock", payload);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao bloquear");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0)
    {
        string rota = etapaAtual switch
        {
            "Aguardando" => "separar",
            "Em Separação" or "Em Separacao" => "AguardandoConfe",
            "Aguardando Conferencia" => "EmConferencia",
            "Aguardando Expedição" or "Aguardando Expedicao" => "Expedido",
            _ => ""
        };

        if (string.IsNullOrEmpty(rota)) return (false, "Status atual inválido.");

        try
        {
            var payload = new { Usuario = usuario, ErrosEvitados = errosEvitados };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/{rota}", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirSeparacaoRotaAsync(Pedido pedido, int tipoRota, string usuario, int errosEvitados = 0)
    {
        try
        {
            var itensRota = (pedido.Itens ?? new List<ItemPedido>())
                .Where(i => i.TipoRota == tipoRota)
                .Select(i => new
                {
                    Id = i.Id,
                    CodItem = i.CodItem,
                    ScansSep = i.BipagensSeparacao,
                    ScansConf = i.BipagensConferencia
                }).ToList();

            var payload = new
            {
                Usuario = usuario,
                ErrosEvitados = errosEvitados,
                TipoRota = tipoRota,
                Itens = itensRota
            };

            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{pedido.IdPedido}/AguardandoConfe", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, double peso = 0, string? observacao = null, string usuario = null, int errosEvitados = 0)
    {
        try
        {
            var payload = new
            {
                Volumes = volumes,
                Peso = peso,
                Observacao = observacao,
                Usuario = usuario,
                ErrosEvitados = errosEvitados
            };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/ConcluirConferencia", payload);
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
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao reverter etapa.");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario, string? motivo = null)
    {
        try
        {
            var payload = new { Usuario = usuario, Motivo = motivo };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/cancelar", payload);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao cancelar pedido.");
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
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao pausar pedido.");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> RetomarPedidoAsync(int id, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/pedidos/{id}/retomar", payload);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao retomar pedido.");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada, string etapa = "", string usuario = "")
    {
        try
        {
            var payload = new
            {
                Id = id,
                PedidoId = id,
                DataHora = dataHoraEstimada,
                DataHoraEstimada = dataHoraEstimada,
                DataHoraEstimadaString = dataHoraEstimada,
                Etapa = etapa,
                Usuario = usuario
            };

            string[] rotasTentativa =
            {
                $"{BaseHost}/api/pedidos/{id}/estimativa",
                $"{BaseHost}/api/pedidos/{id}/responder-estimativa",
                $"{BaseHost}/api/pedidos/{id}/enviar-estimativa",
                $"{BaseHost}/api/pedido/{id}/estimativa",
                $"{BaseHost}/api/pedidos/estimativa",
                $"{BaseHost}/api/pedidos/responder-estimativa"
            };

            HttpResponseMessage? ultimaResposta = null;

            foreach (var rota in rotasTentativa)
            {
                ultimaResposta = await _httpClient.PostAsJsonAsync(rota, payload);
                if (ultimaResposta.IsSuccessStatusCode) return (true, string.Empty);

                if (ultimaResposta.StatusCode != System.Net.HttpStatusCode.NotFound &&
                    ultimaResposta.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed)
                {
                    break;
                }
            }

            if (ultimaResposta != null)
            {
                string detalhe = await ultimaResposta.Content.ReadAsStringAsync();
                return (false, $"HTTP {(int)ultimaResposta.StatusCode}: {detalhe}");
            }

            return (false, "Nenhuma rota de estimativa respondeu na API.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        try
        {
            string url = $"{BaseHost}/api/relatorios/produtividade/excel";
            if (!string.IsNullOrWhiteSpace(operadorFiltro) && operadorFiltro != "Todos os Operadores")
                url += $"?operador={Uri.EscapeDataString(operadorFiltro)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            string filePath = Path.Combine(FileSystem.CacheDirectory, $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            await File.WriteAllBytesAsync(filePath, bytes);
            return filePath;
        }
        catch { return null; }
    }

    public async Task<List<Cotacao>> GetCotacoesPendentesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Cotacao>>($"{BaseHost}/api/cotacoes/pendentes", _jsonOptions) ?? new List<Cotacao>();
        }
        catch { return new List<Cotacao>(); }
    }

    public async Task<(bool Sucesso, string Mensagem)> ResponderCotacaoAsync(int id, int volumes, double pesoTotalKg, string dimensoes, string usuario)
    {
        try
        {
            var payload = new { Volumes = volumes, PesoTotalKg = pesoTotalKg, Dimensoes = dimensoes, Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/cotacoes/{id}/responder", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);

            string detalhe = await response.Content.ReadAsStringAsync();
            return (false, $"HTTP {(int)response.StatusCode}: {detalhe}");
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    // =========================================================================
    // REVERSA (DEVOLUCOES) & MODULO INDEPENDENTE DE AVARIADOS
    // =========================================================================

    public async Task<List<Devolucao>> GetDevolucoesAtivasAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Devolucao>>($"{BaseHost}/api/devolucoes/ativas", _jsonOptions) ?? new List<Devolucao>();
        }
        catch { return new List<Devolucao>(); }
    }

    public async Task<(bool Sucesso, string Mensagem, Devolucao? Devolucao)> ConferirVolumesDevolucaoAsync(int id, string codigoLido, int volumesRecebidos, string usuario)
    {
        try
        {
            var payload = new { CodigoLido = codigoLido, VolumesRecebidos = volumesRecebidos, Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/devolucoes/{id}/conferir-volumes", payload);
            if (response.IsSuccessStatusCode)
            {
                var dev = await response.Content.ReadFromJsonAsync<Devolucao>(_jsonOptions);
                return (true, string.Empty, dev);
            }
            return (false, await response.Content.ReadAsStringAsync(), null);
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}", null); }
    }

    public async Task<(bool Sucesso, bool AprovadaNaConferencia, string Mensagem, Devolucao? Devolucao)> ValidarItensDevolucaoAsync(int id, List<ItemBipadoDevolucaoInput> itensBipados, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario, ItensBipados = itensBipados };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/devolucoes/{id}/validar-itens", payload);
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                bool aprovada = root.TryGetProperty("aprovada", out var propAprov) && propAprov.GetBoolean();
                string msg = root.TryGetProperty("mensagem", out var propMsg) ? propMsg.GetString() ?? string.Empty : string.Empty;
                Devolucao? dev = root.TryGetProperty("devolucao", out var propDev)
                    ? JsonSerializer.Deserialize<Devolucao>(propDev.GetRawText(), _jsonOptions)
                    : null;

                return (true, aprovada, msg, dev);
            }
            return (false, false, await response.Content.ReadAsStringAsync(), null);
        }
        catch (Exception ex) { return (false, false, $"Erro de Conexão: {ex.Message}", null); }
    }

    public async Task<(bool Sucesso, string Mensagem)> RecusarDevolucaoAsync(int id, string motivoRejeicao, string usuario)
    {
        try
        {
            var payload = new { MotivoRejeicao = motivoRejeicao, Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/devolucoes/{id}/recusar", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);
            return (false, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<(bool Sucesso, string Mensagem)> ClassificarItensDevolucaoAsync(int id, List<ItemClassificadoDevolucaoInput> itensClassificados, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario, ItensClassificados = itensClassificados };
            var response = await _httpClient.PatchAsJsonAsync($"{BaseHost}/api/devolucoes/{id}/classificar", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);
            return (false, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }

    public async Task<List<ProdutoAvariado>> GetProdutosAvariadosAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ProdutoAvariado>>($"{BaseHost}/api/devolucoes/avarias", _jsonOptions) ?? new List<ProdutoAvariado>();
        }
        catch { return new List<ProdutoAvariado>(); }
    }

    public async Task<(bool Sucesso, string Mensagem)> RegistrarAvariaEstoqueAsync(string codItem, int quantidade, string motivoAvaria, string usuario)
    {
        try
        {
            var payload = new { CodItem = codItem, Quantidade = quantidade, MotivoAvaria = motivoAvaria, Usuario = usuario };
            var response = await _httpClient.PostAsJsonAsync($"{BaseHost}/api/devolucoes/avarias/registrar-estoque", payload);
            if (response.IsSuccessStatusCode) return (true, string.Empty);
            return (false, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex) { return (false, $"Erro de Conexão: {ex.Message}"); }
    }
}