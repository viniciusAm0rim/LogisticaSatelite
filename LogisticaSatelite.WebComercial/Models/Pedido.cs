namespace LogisticaSatelite.WebComercial.Models;

public class Pedido
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Modal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DataHoraEstimada { get; set; }
    public bool EstimativaSolicitada { get; set; }
    public string? SolicitanteEstimativa { get; set; }
}