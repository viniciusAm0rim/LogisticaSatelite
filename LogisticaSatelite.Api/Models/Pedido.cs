using System.Text.Json.Serialization;

namespace LogisticaSatelite.Api.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string Modal { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Volumes { get; set; }
        public double Peso { get; set; }
        public string? SepUser { get; set; }
        public string? ConfUser { get; set; }
        public string? ExpUser { get; set; }
        public DateTime? SepInicioData { get; set; }
        public DateTime? SepFimData { get; set; }
        public DateTime? ConfInicioData { get; set; }
        public DateTime? ConfFimData { get; set; }
        public DateTime? ExpInicioData { get; set; }
        public DateTime? ExpFimData { get; set; }
        public DateTime? CancelData { get; set; }

        // Coluna única que já existe fisicamente no banco SQLite
        public int ErrosEvitados { get; set; } = 0;
        public string? LockedBy { get; set; }

        // ==========================================
        // CONTROLE DE ESPERA / PAUSA
        // ==========================================
        public bool EmEspera { get; set; } = false;
        public string? EtapaPausada { get; set; }
        public string? PausadoPor { get; set; }
        public DateTime? DataHoraPausa { get; set; }

        public List<RegistroPausa> HistoricoPausas { get; set; } = new();

        // ==========================================
        // PROPRIEDADES DO FLUXO DE ESTIMATIVA
        // ==========================================
        public bool EstimativaSolicitada { get; set; } = false;
        public string? SolicitanteEstimativa { get; set; }
        public string? DataHoraEstimada { get; set; }

        public List<VolumePedido> VolumesPedidos { get; set; } = new();
        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }

    public class RegistroPausa
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }

        [JsonIgnore]
        public Pedido? Pedido { get; set; }

        public string Etapa { get; set; } = string.Empty;
        public string OperadorPausa { get; set; } = string.Empty;
        public DateTime DataHoraPausa { get; set; }
        public string? OperadorRetomada { get; set; }
        public DateTime? DataHoraRetomada { get; set; }
    }
}