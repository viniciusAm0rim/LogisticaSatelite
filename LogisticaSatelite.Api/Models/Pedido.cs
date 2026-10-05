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
        // NOVAS PROPRIEDADES DO FLUXO DE ESTIMATIVA
        // ==========================================
        public bool EstimativaSolicitada { get; set; } = false;
        public string? SolicitanteEstimativa { get; set; }
        public string? DataHoraEstimada { get; set; }

        public List<VolumePedido> VolumesPedidos { get; set; } = new();
        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }
}