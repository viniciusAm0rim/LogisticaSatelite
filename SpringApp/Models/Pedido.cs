using System.ComponentModel.DataAnnotations.Schema;

namespace SpringApp.Models;

public class Pedido
{
    public int Id { get; set; }
    public string Destinatario { get; set; }
    public string Modal { get; set; }
    public string Status { get; set; }
    public int Volumes { get; set; }

    public string SepUser { get; set; }
    public string ConfUser { get; set; }
    public string ExpUser { get; set; }

    // NO APP MAUI (Models/Pedido.cs)
    public DateTime? SepData { get; set; }
    public DateTime? ConfData { get; set; }
    public DateTime? ExpData { get; set; }

    public List<ItemPedido> Itens { get; set; } = new();

    [NotMapped]
    public bool IsExpanded { get; set; }

    [NotMapped]
    public string ProgressoExpedicaoTexto { get; set; }
}