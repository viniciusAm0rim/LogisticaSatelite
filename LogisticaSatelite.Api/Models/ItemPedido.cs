using System.Text.Json.Serialization;

namespace LogisticaSatelite.Api.Models
{
    public class ItemPedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }

        [JsonIgnore]
        public Pedido? Pedido { get; set; }

        public int ProdutoId { get; set; }

        [JsonIgnore]
        public Produto? Produto { get; set; }

        // Dados copiados/preenchidos no momento da criação para histórico
        public string CodItem { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string LocalPicking { get; set; } = string.Empty;
        public string LocalPulmao { get; set; } = string.Empty;

        public int Quantidade { get; set; }
        public int ScansSep { get; set; }
        public int ScansConf { get; set; }
        public int ScansExp { get; set; }
    }
}