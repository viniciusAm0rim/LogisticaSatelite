namespace SpringApp.Models;

public class Produto
{
    public int Id { get; set; }
    public string CodItem { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int UnidadepCx { get; set; }

    // --- ADICIONADO: Endereço de Picking (Essencial para a tela de separação) ---
    public string LocalRua { get; set; } = string.Empty;
    public string LocalPrateleira { get; set; } = string.Empty;
    public string LocalCompartimento { get; set; } = string.Empty;

    // Localização no Pulmão
    public string LocalArea { get; set; } = string.Empty;
    public string LocalPalet { get; set; } = string.Empty;
    public int LocalAndar { get; set; }

    // Controle de Abastecimento
    public int QtdPicking { get; set; }
    public int QtdMinimaPicking { get; set; }
    public bool NeedAbaste { get; set; }
    public int QtdCxAbaste { get; set; }

    // Formatações úteis para a interface
    public string LocalizacaoPulmaoFormatada => $"Pulmão: {LocalArea}-{LocalPalet}-{LocalAndar}";
    public string EstoquePickingFormatado => $"Picking: {QtdPicking} un";
}
