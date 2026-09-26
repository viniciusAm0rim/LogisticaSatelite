
namespace LogisticaSatelite.Api.Models
{
    public class Pedido
    {
            public int Id { get; set; } // Identificador único do pedido
            public string Destinatario { get; set; } = string.Empty; // Nome do destinatário do pedido
            public string Modal { get; set; } = string.Empty; // Modal de transporte do pedido (ex: Retira, Rodoviária, Loja)
            public string Status { get; set; }// Status atual do pedido (ex: Separaçao, Conferencia, Expediçao)
            public int Volumes { get; set; } // Quantidade de volumes do pedido
            public Double Peso { get; set; } // Peso total do pedido em kg
            public string?  SepUser { get; set; } // Matrícula do usuário que realizou a separação do pedido
            public string? ConfUser { get; set; } // Matrícula do usuário que realizou a conferência do pedido
            public string? ExpUser { get; set; } // Matrícula do usuário que realizou a expedição do pedido
            public DateTime? SepData { get; set; } // Data e hora em que a separação do pedido foi realizada
            public DateTime? ConfData { get; set; } // Data e hora em que a conferência do pedido foi realizada
            public DateTime? ExpData { get; set; } // Data e hora em que a expedição do pedido foi realizada
            public string? LockedBy { get; set; } // Matrícula do usuário que realizou o lock do pedido

            public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();// Lista de itens do pedido
            


    }
}
