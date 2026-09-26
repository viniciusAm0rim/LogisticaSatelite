namespace LogisticaSatelite.Api.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public string CodItem { get; set; } = string.Empty; // SKU para bipagem
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int UnidadepCx { get; set; }

        // Localizações simplificadas no padrão solicitado
        public string LocalPicking { get; set; } = string.Empty; // Ex: "R1-A-01" (Rua-Prateleira-Compartimento)
        public string LocalPulmao { get; set; } = string.Empty;  // Ex: "A1-P2"  (Área-Pallet)

        // Controle de Abastecimento
        public int QtdPicking { get; set; }
        public int QtdMinimaPicking { get; set; }
        public bool NeedAbaste => QtdPicking <= QtdMinimaPicking;
        public int QtdCxAbaste { get; set; }
    }
}