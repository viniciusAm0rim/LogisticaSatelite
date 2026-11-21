namespace LogisticaSatelite.Api.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public string CodItem { get; set; } = string.Empty; // SKU unitario para bipagem
        public string CodCaixaFechada { get; set; } = string.Empty; // Codigo de barras da caixa fechada (ex: CX-001 ou DUN-14)
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int UnidadepCx { get; set; }

        // Definido no banco: 0 = Comum (Separacao Normal) | 1 = Area Tecnica
        public int TipoRota { get; set; } = 0;

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