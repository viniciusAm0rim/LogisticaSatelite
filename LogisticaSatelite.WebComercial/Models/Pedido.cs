using LogisticaSatelite.WebComercial.Models;
public class Pedido
{
    public int Id { get; set; }
    public string Destinatario { get; set; }
    public string Modal { get; set; }
    public string Status { get; set; }

    // Adicione estas propriedades para o fluxo de estimativa:
    public bool EstimativaSolicitada { get; set; }
    public string SolicitanteEstimativa { get; set; }
    public string DataHoraEstimada { get; set; }

    public List<ItemPedido> Itens { get; set; } = new();
}