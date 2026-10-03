namespace LogisticaSatelite.WebComercial.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string Destinatario { get; set; }
        public string Status { get; set; }
        public string Modal { get; set; }
    }
}