namespace SpringApp.Models;

public class ItemPedido
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int BipagensSeparacao { get; set; }
    public int BipagensConferencia { get; set; }

    public int SeparacaoPendente => Math.Max(0, Quantidade - BipagensSeparacao);
    public bool ConcluidoSeparacao => BipagensSeparacao >= Quantidade;

    public int ConferenciaPendente => Math.Max(0, Quantidade - BipagensConferencia);
    public bool ConcluidoConferencia => BipagensConferencia >= Quantidade;

}
