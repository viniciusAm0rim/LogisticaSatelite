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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        return await _httpClient.GetFromJsonAsync<List<Pedido>>("api/pedidos", _jsonOptions, cts.Token) ?? new();
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            string endpoint = string.IsNullOrEmpty(status)
                ? "api/pedidos"
                : $"api/pedidos?status={Uri.EscapeDataString(status)}";
            var lista = await _httpClient.GetFromJsonAsync<List<Pedido>>(endpoint, _jsonOptions, cts.Token) ?? new();
            return lista.Where(p => !p.EmEspera).ToList();
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            return await _httpClient.GetFromJsonAsync<Pedido>($"api/pedidos/{id}", _jsonOptions, cts.Token);
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
            "Em Separaçao" => $"api/pedidos/{id}/AguardandoConfe",
            "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
            "Aguardando Expedição" => $"api/pedidos/{id}/Expedido",
            "Aguardando Expediçao" => $"api/pedidos/{id}/Expedido",
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

    public async Task<(bool Sucesso, string Mensagem)> ConcluirSeparacaoRotaAsync(Pedido pedido, int tipoRota, string usuario, int errosEvitados = 0)
    {
        try
        {
            var itensDaRota = pedido.Itens?
                .Where(i => i.TipoRota == tipoRota)
                .Select(i => new
                {
                    Id = i.Id,
                    CodItem = i.CodItem,
                    ScansSep = i.BipagensSeparacao,
                    ScansConf = i.BipagensConferencia
                }).ToList();

            var dto = new
            {
                Usuario = usuario,
                ErrosEvitados = errosEvitados,
                TipoRota = tipoRota,
                Itens = itensDaRota
            };

            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{pedido.IdPedido}/AguardandoConfe", dto, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            string erro = await ExtrairErroHttpAsync(response);
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
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
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao colocar pedido em espera.");
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
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao retomar pedido.");
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
            var dto = new { Usuario = usuario, Motivo = motivo };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/reverter", dto, _jsonOptions);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao reverter etapa.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario, string? motivo = null)
    {
        try
        {
            var dto = new { Usuario = usuario, Motivo = motivo };
            var response = await _httpClient.PatchAsJsonAsync($"api/pedidos/{id}/cancelar", dto, _jsonOptions);
            return response.IsSuccessStatusCode ? (true, string.Empty) : (false, "Erro ao cancelar pedido.");
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
                string caminhoArquivo = Path.Combine(FileSystem.CacheDirectory, "Relatorio_Produtividade.csv");
                File.WriteAllBytes(caminhoArquivo, bytes);
                return caminhoArquivo;
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
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

    public async Task<List<Cotacao>> GetCotacoesPendentesAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            return await _httpClient.GetFromJsonAsync<List<Cotacao>>("api/cotacoes?status=Pendente", _jsonOptions, cts.Token)
                   ?? new List<Cotacao>();
        }
        catch (Exception)
        {
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

            var response = await _httpClient.PatchAsJsonAsync($"api/cotacoes/{id}/responder", payload, _jsonOptions);
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

    public async Task<List<Devolucao>> GetDevolucoesAtivasAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var todas = await _httpClient.GetFromJsonAsync<List<Devolucao>>("api/devolucoes", _jsonOptions, cts.Token) ?? new();
            return todas.Where(d =>
                d.Status.Equals("Autorizada", StringComparison.OrdinalIgnoreCase) ||
                d.Status.Equals("Volumes Conferidos", StringComparison.OrdinalIgnoreCase) ||
                d.Status.Equals("Em Classificação", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        catch (Exception)
        {
            return new();
        }
    }

    public async Task<(bool Sucesso, string Mensagem, Devolucao? Devolucao)> ConferirVolumesDevolucaoAsync(int id, string codigoLido, int volumesRecebidos, string usuario)
    {
        try
        {
            var payload = new { CodigoLido = codigoLido, VolumesRecebidos = volumesRecebidos, Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/conferir-volumes", payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var dev = await response.Content.ReadFromJsonAsync<Devolucao>(_jsonOptions);
                return (true, "Volumes conferidos com sucesso.", dev);
            }

            string erro = await ExtrairErroHttpAsync(response);
            return (false, erro, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}", null);
        }
    }

    public async Task<(bool Sucesso, bool AprovadaNaConferencia, string Mensagem, Devolucao? Devolucao)> ValidarItensDevolucaoAsync(int id, List<ItemBipadoDevolucaoInput> itensBipados, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario, ItensBipados = itensBipados };
            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/validar-itens", payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<ValidacaoItensResponseDto>(_jsonOptions);
                return (true, json?.AprovadaNaConferencia ?? false, json?.Mensagem ?? string.Empty, json?.Devolucao);
            }

            string erro = await ExtrairErroHttpAsync(response);
            return (false, false, erro, null);
        }
        catch (Exception ex)
        {
            return (false, false, $"Erro de Conexão: {ex.Message}", null);
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> RecusarDevolucaoAsync(int id, string motivoRejeicao, string usuario)
    {
        try
        {
            var payload = new { MotivoRejeicao = motivoRejeicao, Usuario = usuario };
            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/recusar", payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, "Devolução recusada na doca.");

            string erro = await ExtrairErroHttpAsync(response);
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> ClassificarItensDevolucaoAsync(int id, List<ItemClassificadoDevolucaoInput> itensClassificados, string usuario)
    {
        try
        {
            var payload = new { Usuario = usuario, ItensClassificados = itensClassificados };
            var response = await _httpClient.PatchAsJsonAsync($"api/devolucoes/{id}/classificar-itens", payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, "Devolução finalizada e estoque atualizado!");

            string erro = await ExtrairErroHttpAsync(response);
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    public async Task<List<ProdutoAvariado>> GetProdutosAvariadosAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            return await _httpClient.GetFromJsonAsync<List<ProdutoAvariado>>("api/avariados", _jsonOptions, cts.Token) ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    public async Task<(bool Sucesso, string Mensagem)> RegistrarAvariaEstoqueAsync(string codItem, int quantidade, string motivoAvaria, string usuario)
    {
        try
        {
            var payload = new { CodItem = codItem, Quantidade = quantidade, MotivoAvaria = motivoAvaria, Usuario = usuario };
            var response = await _httpClient.PostAsJsonAsync("api/avariados/registrar-estoque", payload, _jsonOptions);
            if (response.IsSuccessStatusCode)
                return (true, "Produto avariado registrado com sucesso!");

            string erro = await ExtrairErroHttpAsync(response);
            return (false, erro);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de Conexão: {ex.Message}");
        }
    }

    private async Task<string> ExtrairErroHttpAsync(HttpResponseMessage response)
    {
        try
        {
            var dict = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(_jsonOptions);
            if (dict != null && dict.TryGetValue("message", out var msg))
                return msg?.ToString() ?? $"HTTP {(int)response.StatusCode}";
        }
        catch { }
        return $"HTTP {(int)response.StatusCode}";
    }

    private sealed class ValidacaoItensResponseDto
    {
        public bool AprovadaNaConferencia { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public Devolucao? Devolucao { get; set; }
    }
}

public class ProblemDetailsDto
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
}