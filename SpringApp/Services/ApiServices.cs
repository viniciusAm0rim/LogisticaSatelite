using System.Net.Http.Json;
using System.Text.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class ApiService : IPedidoService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    private string BaseUrl => Preferences.Get("ApiUrl", "http://192.168.15.12:5283").TrimEnd('/');

    public ApiService(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri($"{BaseUrl}/");
            }
        }
        else
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"{BaseUrl}/"),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        var response = await _httpClient.GetAsync("api/pedidos");
        response.EnsureSuccessStatusCode();
        var pedidos = await response.Content.ReadFromJsonAsync<List<Pedido>>(_jsonOptions);
        return pedidos ?? new List<Pedido>();
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/pedidos?status={Uri.EscapeDataString(status)}");
            if (response.IsSuccessStatusCode)
            {
                var pedidos = await response.Content.ReadFromJsonAsync<List<Pedido>>(_jsonOptions);
                return pedidos ?? new List<Pedido>();
            }
            return new List<Pedido>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao filtrar pedidos por status: {ex.Message}");
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
            var response = await _httpClient.GetAsync($"api/pedidos/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Pedido>(_jsonOptions);
            }
            return null;
        }
        catch
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

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0)
    {
        try
        {
            string endpoint = etapaAtual switch
            {
                "Aguardando" => $"api/pedidos/{id}/separar",
                "Em Separação" or "Em Separacao" => $"api/pedidos/{id}/AguardandoConfe",
                "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
                "Aguardando Expedição" or "Aguardando Expedicao" => $"api/pedidos/{id}/Expedido",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(endpoint))
                return (false, "Etapa inválida para avanço.");

            var dto = new
            {
                Usuario = usuario,
                ErrosEvitados = errosEvitados
            };
            var response = await _httpClient.PatchAsJsonAsync(endpoint, dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, $"Erro API: {erro}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
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
                    IsCaixaFechada = i.IsCaixaFechada,
                    ScansSep = i.BipagensSeparacao,
                    ScansConf = i.BipagensConferencia
                }).ToList();

            var dto = new
            {
                Usuario = usuario,
                ErrosEvitados = errosEvitados,
                TipoRota = tipoRota,
                Itens = itensRota
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{pedido.IdPedido}/AguardandoConfe", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, $"Erro API: {erro}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, double peso = 0, string? observacao = null, string usuario = null, int errosEvitados = 0)
    {
        try
        {
            string endpoint = $"api/pedidos/{id}/ConcluirConferencia";
            var dto = new
            {
                Usuario = usuario,
                Volumes = volumes,
                Peso = peso,
                Observacao = observacao,
                ErrosEvitados = errosEvitados
            };
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

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id, string? motivo = null, string? usuario = null)
    {
        try
        {
            var dto = new
            {
                Usuario = usuario,
                Motivo = motivo
            };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/reverter", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            return (false, "Não foi possível reverter o pedido.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario, string? motivo = null)
    {
        try
        {
            var dto = new
            {
                Usuario = usuario,
                Motivo = motivo
            };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/cancelar", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            return (false, "Não foi possível cancelar o pedido.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> PausarPedidoAsync(Pedido pedido, string etapa, string usuario, int errosEvitados = 0, string? motivo = null)
    {
        try
        {
            var dto = new
            {
                Etapa = etapa,
                Usuario = usuario,
                Motivo = motivo,
                ErrosEvitados = errosEvitados,
                Itens = pedido.Itens?.Select(i => new
                {
                    Id = i.Id,
                    CodItem = i.CodItem,
                    IsCaixaFechada = i.IsCaixaFechada,
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

            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{pedido.IdPedido}/pausar", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, $"Erro ao pausar pedido: {erro}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> RetomarPedidoAsync(int id, string usuario)
    {
        try
        {
            var dto = new { Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/retomar", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, $"Erro ao retomar pedido: {erro}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
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
                $"api/pedidos/{id}/estimativa",
                $"api/pedidos/{id}/responder-estimativa",
                $"api/pedidos/{id}/enviar-estimativa",
                $"api/pedido/{id}/estimativa",
                "api/pedidos/estimativa",
                "api/pedidos/responder-estimativa"
            };

            HttpResponseMessage? ultimaResposta = null;

            foreach (var rota in rotasTentativa)
            {
                ultimaResposta = await _httpClient.PostAsJsonAsync(rota, payload, _jsonOptions);
                if (ultimaResposta.IsSuccessStatusCode)
                    return (true, string.Empty);

                if (ultimaResposta.StatusCode != System.Net.HttpStatusCode.NotFound &&
                    ultimaResposta.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed)
                {
                    break;
                }
            }

            if (ultimaResposta != null)
            {
                var erro = await ultimaResposta.Content.ReadAsStringAsync();
                return (false, $"Erro HTTP {(int)ultimaResposta.StatusCode}: {erro}");
            }

            return (false, "Nenhuma rota de estimativa respondeu na API.");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        try
        {
            string url = "api/relatorios/produtividade/excel";
            if (!string.IsNullOrWhiteSpace(operadorFiltro) && operadorFiltro != "Todos os Operadores")
            {
                url += $"?operador={Uri.EscapeDataString(operadorFiltro)}";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            string fileName = $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, bytes);
            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao baixar relatório: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Cotacao>> GetCotacoesPendentesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/cotacoes/pendentes");
            if (response.IsSuccessStatusCode)
            {
                var lista = await response.Content.ReadFromJsonAsync<List<Cotacao>>(_jsonOptions);
                return lista ?? new List<Cotacao>();
            }
            return new List<Cotacao>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao buscar cotações: {ex.Message}");
            return new List<Cotacao>();
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ResponderCotacaoAsync(int id, int volumes, double pesoTotalKg, string dimensoes, string usuario)
    {
        try
        {
            var dto = new
            {
                Volumes = volumes,
                PesoTotalKg = pesoTotalKg,
                Dimensoes = dimensoes,
                Usuario = usuario
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/cotacoes/{id}/responder", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, $"Erro ao responder cotação: {erro}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    // =========================================================================
    // REVERSA (DEVOLUCOES) & MODULO INDEPENDENTE DE AVARIADOS
    // =========================================================================

    public async Task<List<Devolucao>> GetDevolucoesAtivasAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/devolucoes/ativas");
            if (response.IsSuccessStatusCode)
            {
                var lista = await response.Content.ReadFromJsonAsync<List<Devolucao>>(_jsonOptions);
                return lista ?? new List<Devolucao>();
            }
            return new List<Devolucao>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao buscar devoluções ativas: {ex.Message}");
            return new List<Devolucao>();
        }
    }

    public async Task<(bool Sucesso, string Mensagem, Devolucao? Devolucao)> ConferirVolumesDevolucaoAsync(int id, string codigoLido, int volumesRecebidos, string usuario)
    {
        try
        {
            var dto = new
            {
                CodigoLido = codigoLido,
                VolumesRecebidos = volumesRecebidos,
                Usuario = usuario
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/conferir-volumes", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var dev = await response.Content.ReadFromJsonAsync<Devolucao>(_jsonOptions);
                return (true, string.Empty, dev);
            }

            var erro = await response.Content.ReadAsStringAsync();
            return (false, erro, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}", null);
        }
    }

    public async Task<(bool Sucesso, bool AprovadaNaConferencia, string Mensagem, Devolucao? Devolucao)> ValidarItensDevolucaoAsync(int id, List<ItemBipadoDevolucaoInput> itensBipados, string usuario)
    {
        try
        {
            var dto = new
            {
                Usuario = usuario,
                ItensBipados = itensBipados
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/validar-itens", dto, _jsonOptions);
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

            var erro = await response.Content.ReadAsStringAsync();
            return (false, false, erro, null);
        }
        catch (Exception ex)
        {
            return (false, false, $"Erro de conexão: {ex.Message}", null);
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> RecusarDevolucaoAsync(int id, string motivoRejeicao, string usuario)
    {
        try
        {
            var dto = new
            {
                MotivoRejeicao = motivoRejeicao,
                Usuario = usuario
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/recusar", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ClassificarItensDevolucaoAsync(int id, List<ItemClassificadoDevolucaoInput> itensClassificados, string usuario)
    {
        try
        {
            var dto = new
            {
                Usuario = usuario,
                ItensClassificados = itensClassificados
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/classificar", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task<List<ProdutoAvariado>> GetProdutosAvariadosAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/avariados");
            if (response.IsSuccessStatusCode)
            {
                var lista = await response.Content.ReadFromJsonAsync<List<ProdutoAvariado>>(_jsonOptions);
                return lista ?? new List<ProdutoAvariado>();
            }
            return new List<ProdutoAvariado>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao buscar avarias: {ex.Message}");
            return new List<ProdutoAvariado>();
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> RegistrarAvariaEstoqueAsync(string codItem, int quantidade, string motivoAvaria, string usuario)
    {
        try
        {
            var dto = new
            {
                CodItem = codItem,
                Quantidade = quantidade,
                MotivoAvaria = motivoAvaria,
                Usuario = usuario
            };

            var response = await _httpClient.PostAsJsonAsync("api/avariados/registrar-estoque", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var erro = await response.Content.ReadAsStringAsync();
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }
}