namespace LogisticaSatelite.WebComercial.Models;

public class AdmDashboardDto
{
    public List<string> FuncionariosDisponiveis { get; set; } = new();
    public string EscopoSelecionado { get; set; } = "Todos";

    // Cards Principais
    public int PedidosRecebidos { get; set; }
    public int PedidosEnviados { get; set; }
    public double TempoMedioPorPedidoMin { get; set; }
    public int ErrosEvitados { get; set; }

    // Acurácia de tempo por finalização (término da conferência)
    public double AcuraciaConferenciaPercentual { get; set; }
    public int ConferenciasNoPrazo { get; set; }
    public int ConferenciasTotais { get; set; }

    // Pausas
    public int QuantidadePausas { get; set; }
    public double TempoTotalPausadoMin { get; set; }

    // Gráfico 1: Etapas finalizadas por funcionário
    public List<EtapasFuncionarioItem> EtapasPorFuncionario { get; set; } = new();

    // Gráfico 2: Erros em relação ao limite máximo (10)
    public int ErrosTotais { get; set; }
    public int LimiteMaximoErros { get; set; } = 10;

    // Gráfico 3: Acurácia de pedidos vs Meta de 98%
    public double MetaAcuraciaPercentual { get; set; } = 98.0;
    public double AcuraciaTotalPedidosPercentual { get; set; }
    public int PedidosFinalizadosNoPrazo { get; set; }
    public int PedidosFinalizadosForaPrazo { get; set; }
}

public class EtapasFuncionarioItem
{
    public string NomeFuncionario { get; set; } = string.Empty;
    public int SeparacoesFinalizadas { get; set; }
    public int ConferenciasFinalizadas { get; set; }
    public int ExpedicoesFinalizadas { get; set; }
    public int TotalEtapas => SeparacoesFinalizadas + ConferenciasFinalizadas + ExpedicoesFinalizadas;
}
