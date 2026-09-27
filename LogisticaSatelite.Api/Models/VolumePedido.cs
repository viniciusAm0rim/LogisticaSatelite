namespace LogisticaSatelite.Api.Models
{
    public class VolumePedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public string CodigoVolume { get; set; } = string.Empty; // Ex: c1p1v1
        public bool BipadoExpedicao { get; set; } = false;
    }
}