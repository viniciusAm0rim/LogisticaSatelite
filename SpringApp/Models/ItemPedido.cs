using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpringApp.Models;

public class ItemPedido : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }

    // Propriedades simplificadas vindas da API
    public string CodItem { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string LocalPicking { get; set; } = string.Empty;
    public string LocalPulmao { get; set; } = string.Empty;
    public int Quantidade { get; set; }

    private int _bipagensSeparacao;
    public int BipagensSeparacao
    {
        get => _bipagensSeparacao;
        set
        {
            if (_bipagensSeparacao != value)
            {
                _bipagensSeparacao = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SeparacaoPendente));
                OnPropertyChanged(nameof(ProgressoSeparacaoTexto));
                OnPropertyChanged(nameof(BotaoSeparacaoTexto));
                OnPropertyChanged(nameof(BotaoSeparacaoCorFundo));
                OnPropertyChanged(nameof(BotaoSeparacaoCorTexto));
            }
        }
    }

    private int _bipagensConferencia;
    public int BipagensConferencia
    {
        get => _bipagensConferencia;
        set
        {
            if (_bipagensConferencia != value)
            {
                _bipagensConferencia = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConferenciaPendente));
                OnPropertyChanged(nameof(ProgressoConferenciaTexto));
                OnPropertyChanged(nameof(BotaoConferenciaTexto));
                OnPropertyChanged(nameof(BotaoConferenciaCorFundo));
                OnPropertyChanged(nameof(BotaoConferenciaCorTexto));
            }
        }
    }

    public int SeparacaoPendente => Math.Max(0, Quantidade - BipagensSeparacao);
    public int ConferenciaPendente => Math.Max(0, Quantidade - BipagensConferencia);

    public string ProgressoSeparacaoTexto => $"Progresso: {BipagensSeparacao} de {Quantidade}";
    public string BotaoSeparacaoTexto => SeparacaoPendente <= 0 ? "✔ Concluído" : $"Faltam {SeparacaoPendente}";
    public string BotaoSeparacaoCorFundo => SeparacaoPendente <= 0 ? "#dcfce7" : "#fef3c7";
    public string BotaoSeparacaoCorTexto => SeparacaoPendente <= 0 ? "#15803d" : "#b45309";

    public string ProgressoConferenciaTexto => $"Progresso: {BipagensConferencia} de {Quantidade}";
    public string BotaoConferenciaTexto => ConferenciaPendente <= 0 ? "✔ Concluído" : $"Faltam {ConferenciaPendente}";
    public string BotaoConferenciaCorFundo => ConferenciaPendente <= 0 ? "#dcfce7" : "#fef3c7";
    public string BotaoConferenciaCorTexto => ConferenciaPendente <= 0 ? "#15803d" : "#b45309";
}