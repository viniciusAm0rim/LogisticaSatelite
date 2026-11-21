using System.Text.Json.Serialization;

namespace SpringApp.Models;

public class Pedido
{
    [JsonPropertyName("id")]
    public int IdPedido { get; set; }

    [JsonPropertyName("destinatario")]
    public string Cliente { get; set; }

    [JsonPropertyName("modal")]
    public string Modal { get; set; }

    [JsonPropertyName("nomeTransportadora")]
    public string NomeTransportadora { get; set; }

    [JsonPropertyName("codTransportadora")]
    public string CodTransportadora { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Aguardando";

    [JsonPropertyName("volumes")]
    public int Volumes { get; set; }

    [JsonPropertyName("peso")]
    public double Peso { get; set; }

    [JsonPropertyName("observacaoConferencia")]
    public string? ObservacaoConferencia { get; set; }

    [JsonPropertyName("errosEvitados")]
    public int ErrosEvitados { get; set; }

    [JsonPropertyName("sepUser")] public string SepUser { get; set; }
    [JsonPropertyName("confUser")] public string ConfUser { get; set; }
    [JsonPropertyName("expUser")] public string ExpUser { get; set; }

    [JsonPropertyName("sepInicioData")] public DateTime? SepInicioData { get; set; }
    [JsonPropertyName("sepFimData")] public DateTime? SepFimData { get; set; }

    [JsonPropertyName("confInicioData")] public DateTime? ConfInicioData { get; set; }
    [JsonPropertyName("confFimData")] public DateTime? ConfFimData { get; set; }

    [JsonPropertyName("expInicioData")] public DateTime? ExpInicioData { get; set; }
    [JsonPropertyName("expFimData")] public DateTime? ExpFimData { get; set; }

    [JsonPropertyName("cancelData")] public DateTime? CancelData { get; set; }

    [JsonPropertyName("cancelUser")] public string? CancelUser { get; set; }

    [JsonPropertyName("motivoCancelamento")] public string? MotivoCancelamento { get; set; }

    [JsonPropertyName("emEspera")] public bool EmEspera { get; set; }
    [JsonPropertyName("etapaPausada")] public string EtapaPausada { get; set; }
    [JsonPropertyName("pausadoPor")] public string PausadoPor { get; set; }
    [JsonPropertyName("dataHoraPausa")] public DateTime? DataHoraPausa { get; set; }

    [JsonPropertyName("historicoPausas")]
    public List<RegistroPausa> HistoricoPausas { get; set; } = new();

    [JsonIgnore]
    public string ResumoProgressoEspera
    {
        get
        {
            if (EtapaPausada == "Separação" && Itens != null)
            {
                int lidos = Itens.Sum(i => i.BipagensSeparacao);
                int total = Itens.Sum(i => i.Quantidade);
                return $"Separados: {lidos} de {total} itens";
            }
            if (EtapaPausada == "Conferência" && Itens != null)
            {
                int lidos = Itens.Sum(i => i.BipagensConferencia);
                int total = Itens.Sum(i => i.Quantidade);
                return $"Conferidos: {lidos} de {total} itens";
            }
            if (EtapaPausada == "Expedição" && VolumesPedidos != null)
            {
                int lidos = VolumesPedidos.Count(v => v.BipadoExpedicao);
                int total = VolumesPedidos.Count;
                return $"Volumes bipados: {lidos} de {total}";
            }
            return "Em espera";
        }
    }

    [JsonPropertyName("volumesPedidos")]
    public List<VolumePedidoDto> VolumesPedidos { get; set; } = new();

    [JsonPropertyName("itens")]
    public List<ItemPedido> Itens { get; set; } = new();

    [JsonIgnore] public bool IsExpanded { get; set; }

    [JsonPropertyName("estimativaSolicitada")]
    public bool EstimativaSolicitada { get; set; }

    [JsonPropertyName("solicitanteEstimativa")]
    public string SolicitanteEstimativa { get; set; }

    [JsonPropertyName("dataHoraEstimada")]
    public string DataHoraEstimada { get; set; }

    [JsonPropertyName("estimativaPendenteSeparacao")]
    public bool EstimativaPendenteSeparacao { get; set; }

    [JsonPropertyName("estimativaSeparacao")]
    public string EstimativaSeparacao { get; set; }

    [JsonPropertyName("estimativaSeparacaoUser")]
    public string EstimativaSeparacaoUser { get; set; }

    [JsonPropertyName("estimativaPendenteConferencia")]
    public bool EstimativaPendenteConferencia { get; set; }

    [JsonPropertyName("estimativaConferencia")]
    public string EstimativaConferencia { get; set; }

    [JsonPropertyName("estimativaConferenciaUser")]
    public string EstimativaConferenciaUser { get; set; }

    [JsonIgnore]
    public string DescricaoPendenciaEstimativa
    {
        get
        {
            if (EstimativaPendenteSeparacao)
                return "Aguardando estimativa";

            if (EstimativaPendenteConferencia)
            {
                if (string.IsNullOrWhiteSpace(EstimativaSeparacao))
                    return "Aguardando estimativa";

                string autor = string.IsNullOrWhiteSpace(EstimativaSeparacaoUser) ? "Separador" : EstimativaSeparacaoUser;
                return $"{autor}: {EstimativaSeparacao}";
            }

            return "Aguardando estimativa";
        }
    }
}

public class RegistroPausa
{
    [JsonPropertyName("etapa")]
    public string Etapa { get; set; } = string.Empty;

    [JsonPropertyName("operadorPausa")]
    public string OperadorPausa { get; set; } = string.Empty;

    [JsonPropertyName("dataHoraPausa")]
    public DateTime DataHoraPausa { get; set; }

    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }

    [JsonPropertyName("operadorRetomada")]
    public string? OperadorRetomada { get; set; }

    [JsonPropertyName("dataHoraRetomada")]
    public DateTime? DataHoraRetomada { get; set; }
}

public class VolumePedidoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigoVolume")]
    public string CodigoVolume { get; set; }

    [JsonPropertyName("bipadoExpedicao")]
    public bool BipadoExpedicao { get; set; }
}