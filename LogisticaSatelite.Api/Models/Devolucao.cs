using System.Text.Json.Serialization;

namespace LogisticaSatelite.Api.Models
{
    public class Devolucao
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string DocumentoCliente { get; set; } = string.Empty;

        // false = Cliente Fisico (sem NF, usa CodigoAutorizacao gerado pelo ADM)
        // true = Cliente Juridico (com NF, operador bipa NumeroNotaDevolucao)
        public bool EmiteNotaFiscal { get; set; } = false;
        public string? NumeroNotaDevolucao { get; set; }
        public string? CodigoAutorizacao { get; set; }

        public string MotivoGeral { get; set; } = string.Empty;

        // Status: "Pendente Analise", "Autorizada", "Rejeitada", "Volumes Conferidos", "Recusada na Doca", "Em Classificação", "Concluida"
        public string Status { get; set; } = "Pendente Analise";
        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        public string? AnalisadoPor { get; set; }
        public DateTime? DataAnalise { get; set; }
        public string? MotivoRejeicao { get; set; }

        public int VolumesInformados { get; set; } = 1;
        public int? VolumesRecebidos { get; set; }
        public string? ConferidoPor { get; set; }
        public DateTime? DataConferencia { get; set; }

        public List<ItemDevolucao> Itens { get; set; } = new();
    }

    public class ItemDevolucao
    {
        public int Id { get; set; }
        public int DevolucaoId { get; set; }

        [JsonIgnore]
        public Devolucao? Devolucao { get; set; }

        public string CodItem { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeSolicitada { get; set; }
        public int QuantidadeBipada { get; set; } = 0;
        public int QuantidadeBomEstado { get; set; } = 0;
        public int QuantidadeAvariada { get; set; } = 0;
        public string? MotivoAvaria { get; set; }
    }

    public class ProdutoAvariado
    {
        public int Id { get; set; }
        public string CodItem { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }

        // "Estoque Interno" (bipado direto no estoque) ou "Devolução #0001"
        public string Origem { get; set; } = "Estoque Interno";
        public int? DevolucaoId { get; set; }
        public int? PedidoId { get; set; }
        public string MotivoAvaria { get; set; } = string.Empty;
        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime DataRegistro { get; set; } = DateTime.Now;
    }
}