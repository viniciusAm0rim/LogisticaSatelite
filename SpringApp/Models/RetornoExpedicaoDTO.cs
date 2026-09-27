namespace SpringApp.Models;

public class RetornoExpedicaoDto
{
    public int IdPedidoMae { get; set; } // Alterado para int para bater com seu banco
    public string StatusFinal { get; set; }
    public int QuantidadeVolumes { get; set; }
    public string OperadorResponsavel { get; set; }
    public DateTime DataConclusao { get; set; }
}