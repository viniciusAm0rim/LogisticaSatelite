namespace SpringApp.Models;

public class Pedido
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Volumes { get; set; }
    public double Peso { get; set; }

 
    public string? SepUser { get; set; }
    public string? ConfUser { get; set; }
    public string? ExpUser { get; set; }

    public string StatusProgressoVolume { get; set; } = string.Empty;
    public List<ItemPedido> Itens { get; set; } = new();
}