using System.Text.Json.Serialization;

namespace LogisticaSatelite.WebComercial.Models;

public class PedidoDto
{
    public int Id { get; set; }

    [JsonPropertyName("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    // Propriedade de atalho para exibição na tabela
    [JsonIgnore]
    public string NomeCliente => Destinatario;

    [JsonPropertyName("modal")]
    public string Modal { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("estimativaSolicitada")]
    public bool EstimativaSolicitada { get; set; }

    [JsonPropertyName("solicitanteEstimativa")]
    public string? SolicitanteEstimativa { get; set; }

    [JsonPropertyName("dataHoraEstimada")]
    public string? DataHoraEstimada { get; set; }
}