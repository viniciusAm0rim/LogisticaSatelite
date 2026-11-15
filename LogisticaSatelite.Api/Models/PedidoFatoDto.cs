namespace LogisticaSatelite.Api.Models
{
    public class PedidoFatoDto
    {
        public int IdPedido { get; set; }
        public DateTime DataOperacao { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Modal { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Volumes { get; set; }
        public bool EmEspera { get; set; }
        public string? SepUser { get; set; }
        public string? ConfUser { get; set; }
        public string? ExpUser { get; set; }
        public double DiasUteisConferencia { get; set; }
        public int QtdPausas { get; set; }
        public double TempoPausadoMin { get; set; }
        public double TempoLiquidoMin { get; set; }
        public int ErrosEvitados { get; set; }
        public int ErrosReais { get; set; }
    }
}