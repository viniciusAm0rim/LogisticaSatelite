using System.Text.Json.Serialization;

namespace LogisticaSatelite.WebComercial.Models;

public class PedidoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [JsonIgnore]
    public string NomeCliente => string.IsNullOrWhiteSpace(Destinatario) ? "Cliente não informado" : Destinatario;

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

    // Campos de controle das 2 etapas (o comercial visualiza apenas o retorno do conferente)
    [JsonPropertyName("estimativaPendenteSeparacao")]
    public bool EstimativaPendenteSeparacao { get; set; }

    [JsonPropertyName("estimativaPendenteConferencia")]
    public bool EstimativaPendenteConferencia { get; set; }

    [JsonPropertyName("estimativaConferencia")]
    public string? EstimativaConferencia { get; set; }

    [JsonPropertyName("estimativaConferenciaUser")]
    public string? EstimativaConferenciaUser { get; set; }

    // Retorna o horario final apenas quando o conferente ja respondeu
    [JsonIgnore]
    public string? EstimativaFinalConferente =>
        !string.IsNullOrWhiteSpace(EstimativaConferencia)
            ? EstimativaConferencia
            : (!EstimativaSolicitada && !EstimativaPendenteSeparacao && !EstimativaPendenteConferencia && !string.IsNullOrWhiteSpace(DataHoraEstimada)
                ? DataHoraEstimada
                : null);

    [JsonIgnore]
    public bool PossuiEstimativaEmAndamento =>
        EstimativaSolicitada || EstimativaPendenteSeparacao || EstimativaPendenteConferencia;

    [JsonPropertyName("emEspera")]
    public bool EmEspera { get; set; }

    [JsonPropertyName("etapaPausada")]
    public string? EtapaPausada { get; set; }

    [JsonPropertyName("errosEvitados")]
    public int ErrosEvitados { get; set; }

    [JsonPropertyName("erros")]
    public int ErrosRegistrados { get; set; }

    [JsonPropertyName("sepUser")]
    public string? SepUser { get; set; }

    [JsonPropertyName("sepInicioData")]
    public DateTime? SepInicioData { get; set; }

    [JsonPropertyName("sepFimData")]
    public DateTime? SepFimData { get; set; }

    [JsonPropertyName("confUser")]
    public string? ConfUser { get; set; }

    [JsonPropertyName("confInicioData")]
    public DateTime? ConfInicioData { get; set; }

    [JsonPropertyName("confFimData")]
    public DateTime? ConfFimData { get; set; }

    [JsonPropertyName("expUser")]
    public string? ExpUser { get; set; }

    [JsonPropertyName("expInicioData")]
    public DateTime? ExpInicioData { get; set; }

    [JsonPropertyName("expFimData")]
    public DateTime? ExpFimData { get; set; }

    [JsonPropertyName("historicoPausas")]
    public List<RegistroPausaDto> HistoricoPausas { get; set; } = new();
}

public class RegistroPausaDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("etapa")]
    public string Etapa { get; set; } = string.Empty;

    [JsonPropertyName("operadorPausa")]
    public string OperadorPausa { get; set; } = string.Empty;

    [JsonPropertyName("dataHoraPausa")]
    public DateTime DataHoraPausa { get; set; }

    [JsonPropertyName("operadorRetomada")]
    public string? OperadorRetomada { get; set; }

    [JsonPropertyName("dataHoraRetomada")]
    public DateTime? DataHoraRetomada { get; set; }
}