using System.Text.Json.Serialization;

namespace LogisticaSatelite.Api.Models
{
    public class Cotacao
    {
        public int Id { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string? Observacao { get; set; }
        public string Solicitante { get; set; } = "Comercial";
        public DateTime DataSolicitacao { get; set; } = DateTime.Now;

        // "Pendente" ou "Respondida"
        public string Status { get; set; } = "Pendente";

        // Dados preenchidos pelo armazem (SpringApp)
        public int? Volumes { get; set; }
        public double? PesoTotalKg { get; set; }
        public string? Dimensoes { get; set; }
        public string? RespondidoPor { get; set; }
        public DateTime? DataResposta { get; set; }

        public List<ItemCotacao> Itens { get; set; } = new();
    }

    public class ItemCotacao
    {
        public int Id { get; set; }
        public int CotacaoId { get; set; }

        [JsonIgnore]
        public Cotacao? Cotacao { get; set; }

        public string CodItem { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
    }
}
