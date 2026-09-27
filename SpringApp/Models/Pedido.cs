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

    [JsonPropertyName("volumesPedidos")]
    public List<VolumePedidoDto> VolumesPedidos { get; set; } = new();

    [JsonPropertyName("itens")]
    public List<ItemPedido> Itens { get; set; } = new();

    [JsonIgnore] public bool IsExpanded { get; set; }
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