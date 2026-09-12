using SpringApp.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SpringApp.Services;

public class ApiServices
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

    public async Task<List<Pedido>> ObterPedidosAsync()
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

    public async Task<(bool Sucesso, string MensagemErro)> AvancarEtapaAsync(int id, string statusAtual)
    {
        string endpoint = statusAtual switch
        {
            "Aguardando" => $"api/pedidos/{id}/separar",
            "Em Separaçao" => $"api/pedidos/{id}/AguardandoConfe",
            "Aguardando Conferencia" => $"api/pedidos/{id}/EmConferencia",
            "Em Conferencia" => $"api/pedidos/{id}/AguardandoExped",
            "Aguardando Expediçao" => $"api/pedidos/{id}/Expedido",
            _ => string.Empty // variavel que 
        };

        if (string.IsNullOrEmpty(endpoint))
        {
            return (false, "Este pedido já está em um estado final.");
        }

        var response = await _httpClient.PatchAsync(endpoint, null);

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
}

public class ProblemDetailsDto
{
    public string Title { get; set; }
    public int Status { get; set; }
    public string Detail { get; set; }
}
