using System.Text.Json.Serialization;

namespace SpringApp.Models;

public class Cotacao
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

    [JsonPropertyName("itens")]
    public List<ItemCotacao> Itens { get; set; } = new();

    [JsonIgnore]
    public bool TemObservacao => !string.IsNullOrWhiteSpace(Observacao);

    [JsonIgnore]
    public string ResumoItens
    {
        get
        {
            if (Itens == null || Itens.Count == 0)
                return "0 itens";

            int totalUnidades = Itens.Sum(i => i.Quantidade);
            string labelItens = Itens.Count == 1 ? "item" : "itens";
            return $"{Itens.Count} {labelItens} ({totalUnidades} un.)";
        }
    }
}

public class ItemCotacao
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
