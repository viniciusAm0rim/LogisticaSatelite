using System.Text.Json.Serialization;

namespace LogisticaSatelite.WebComercial.Models;

public class CotacaoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("cliente")]
    public string Cliente { get; set; } = string.Empty;

    [JsonPropertyName("observacao")]
    public string? Observacao { get; set; }

    [JsonPropertyName("solicitante")]
    public string Solicitante { get; set; } = string.Empty;

    [JsonPropertyName("dataSolicitacao")]
    public DateTime DataSolicitacao { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pendente";

    [JsonPropertyName("volumes")]
    public int? Volumes { get; set; }

    [JsonPropertyName("pesoTotalKg")]
    public double? PesoTotalKg { get; set; }

    [JsonPropertyName("dimensoes")]
    public string? Dimensoes { get; set; }

    [JsonPropertyName("respondidoPor")]
    public string? RespondidoPor { get; set; }

    [JsonPropertyName("dataResposta")]
    public DateTime? DataResposta { get; set; }

    [JsonPropertyName("itens")]
    public List<ItemCotacaoDto> Itens { get; set; } = new();

    [JsonIgnore]
    public bool EstaRespondida => Status.Equals("Respondida", StringComparison.OrdinalIgnoreCase);
}

public class ItemCotacaoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codItem")]
    public string CodItem { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }
}

public class ProdutoCatalogoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codItem")]
    public string CodItem { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;
}

public class NovaCotacaoRequestDto
{
    public string Cliente { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public string Solicitante { get; set; } = "Comercial";
    public List<ItemCotacaoDto> Itens { get; set; } = new();
}