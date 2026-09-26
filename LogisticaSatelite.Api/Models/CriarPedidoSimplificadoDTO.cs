namespace LogisticaSatelite.Api.Models
{
    public class CriarPedidoSimplificadoDTO
    {
        public string Destinatario { get; set; } = string.Empty; // Nome ou Código do cliente
        public string Modal { get; set; } = string.Empty;        // "Loja", "Transportadora", "Retira"
        public string? CodigoTransportadora { get; set; }       // Preenchido apenas se modal for transportadora

        public List<ItemPedidoInputDTO> Itens { get; set; } = new();
    }

    public class ItemPedidoInputDTO
    {
        public string CodItem { get; set; } = string.Empty;     // Código/SKU do produto enviado pelo app
        public int Quantidade { get; set; }
    }
}
