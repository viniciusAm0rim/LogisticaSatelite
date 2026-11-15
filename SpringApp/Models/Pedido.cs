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

    // Registro da data/hora de cancelamento
    [JsonPropertyName("cancelData")] public DateTime? CancelData { get; set; }

    // Controle de Espera / Pausa
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

    // Propriedades para o fluxo de Estimativa entre Comercial e Supervisor
    [JsonPropertyName("estimativaSolicitada")]
    public bool EstimativaSolicitada { get; set; }

    [JsonPropertyName("solicitanteEstimativa")]
    public string SolicitanteEstimativa { get; set; }

    [JsonPropertyName("dataHoraEstimada")]
    public string DataHoraEstimada { get; set; }
}

public class RegistroPausa
{
    public string Etapa { get; set; } = string.Empty;
    public string OperadorPausa { get; set; } = string.Empty;
    public DateTime DataHoraPausa { get; set; }
    public string? OperadorRetomada { get; set; }
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