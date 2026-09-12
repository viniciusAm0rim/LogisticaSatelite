using System.Text.Json.Serialization;

namespace LogisticaSatelite.Api.Models
{
    public class ItemPedido
    {
        // relacionamento com o Pedido Base
        public int PedidoId { get; set; } // Identificador do pedido ao qual o item pertence

        // Propriedades do ItemPedido
        public int Id { get; set; } // Identificador único do item do pedido
        public string nome { get; set; } = string.Empty; // Nome do item do pedido
        public int Quantidade { get; set; } //
        public string descricao { get; set;} = string.Empty;// Descrição do item do pedido
        public string CodItem { get; set; } = string.Empty;// Código do item do pedido
        public string LocalRua {  get; set; } = string.Empty;// Identificaçao da Rua onde o item se encontra
        public string LocalPrateleira { get; set; } = string.Empty;// Identificaçao da Prateleira onde o item se encontra
        public string LocalCompartimento { get; set; } = string.Empty;// Identificaçao do Compartimento onde o item se encontra na Prateleira

        // Localização do item no depósito
        public int LocalAndar { get; set; } // Identificaçao do Andar onde o item se encontra
        public string LocalArea { get; set; } = string.Empty;// Identificaçao da Área onde a caixa do item se encontra
        public string LocalPalet { get; set; } = string.Empty;// Identificaçao do Palet onde a caixa do item se encontra
        public int UnidadepCx { get; set; } // Quantidade de unidades do item em uma caixa fechada

        // Quantidade de scans realizados no Item durante sua separação, conferência e expedição
        public int ScansSep { get; set; } // Quantidade de scans realizados no Item durante sua separação
        public int ScansConf { get; set; } // Quantidade de scans realizados no Item durante sua conferência
        public int ScansExp { get; set; } // Quantidade de scans realizados nos volumes do pedido durante sua expedição

        // Propriedades de controle de Abastecimento
        public bool NeedAbaste { get; set; } = false;// Indica se o item precisa de abastecimento
        public bool Abastecido { get; set; } = false; // Indica se o item já foi abastecido
        public int QtdCxAbaste { get; set; } // Quantidade de caixas para abastecimento



    }
}
