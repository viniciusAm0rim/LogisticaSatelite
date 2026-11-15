using System.Net.Http.Json;
using System.Text.Json;
using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Producao;

public class ProducaoAdmService : IAdmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProducaoAdmService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProducaoAdmService(HttpClient httpClient, ILogger<ProducaoAdmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ResultadoDashboardAdm> ObterDashboardAsync(string funcionario = "Todos")
    {
        try
        {
            var todosPedidos = await _httpClient.GetFromJsonAsync<List<PedidoDto>>("api/Pedidos", _jsonOptions)
                               ?? new List<PedidoDto>();

            var dashboard = CalcularIndicadores(todosPedidos, funcionario);
            return new ResultadoDashboardAdm(true, dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar indicadores administrativos da API.");
            return new ResultadoDashboardAdm(
                false,
                new AdmDashboardDto { EscopoSelecionado = funcionario },
                "Falha de comunicação com a API ao carregar os indicadores administrativos."
            );
        }
    }

    internal static AdmDashboardDto CalcularIndicadores(List<PedidoDto> todosPedidos, string funcionarioSelecionado)
    {
        const double slaConferenciaMin = 30.0;
        const double slaPedidoTotalMin = 75.0;

        var funcionarios = todosPedidos
            .SelectMany(p => new[] { p.SepUser, p.ConfUser, p.ExpUser }
                .Concat(p.HistoricoPausas.Select(h => h.OperadorPausa)))
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u)
            .ToList();

        bool filtrarPorPessoa = !string.IsNullOrWhiteSpace(funcionarioSelecionado) &&
                                !funcionarioSelecionado.Equals("Todos", StringComparison.OrdinalIgnoreCase);

        var pedidosEscopo = filtrarPorPessoa
            ? todosPedidos.Where(p =>
                string.Equals(p.SepUser, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.ConfUser, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.ExpUser, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase) ||
                p.HistoricoPausas.Any(h =>
                    string.Equals(h.OperadorPausa, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(h.OperadorRetomada, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase)))
              .ToList()
            : todosPedidos;

        int recebidos = pedidosEscopo.Count;
        int enviados = pedidosEscopo.Count(p => p.Status.Equals("Expedido", StringComparison.OrdinalIgnoreCase));
        int errosEvitados = pedidosEscopo.Sum(p => p.ErrosEvitados);

        var temposPedidos = new List<double>();
        int confTotais = 0;
        int confNoPrazo = 0;
        int finalizadosNoPrazo = 0;
        int finalizadosForaPrazo = 0;

        foreach (var p in pedidosEscopo)
        {
            double minPausadoPedido = p.HistoricoPausas.Sum(h =>
                Math.Max(0, ((h.DataHoraRetomada ?? DateTime.Now) - h.DataHoraPausa).TotalMinutes));

            double minSep = (p.SepInicioData.HasValue && p.SepFimData.HasValue)
                ? Math.Max(0, (p.SepFimData.Value - p.SepInicioData.Value).TotalMinutes)
                : 0;

            double minConf = (p.ConfInicioData.HasValue && p.ConfFimData.HasValue)
                ? Math.Max(0, (p.ConfFimData.Value - p.ConfInicioData.Value).TotalMinutes)
                : 0;

            double minExp = (p.ExpInicioData.HasValue && p.ExpFimData.HasValue)
                ? Math.Max(0, (p.ExpFimData.Value - p.ExpInicioData.Value).TotalMinutes)
                : 0;

            double tempoLiquido = Math.Max(0, (minSep + minConf + minExp) - minPausadoPedido);
            if (tempoLiquido > 0)
            {
                temposPedidos.Add(tempoLiquido);
            }

            // Acurácia por finalização (término da conferência)
            if (p.ConfInicioData.HasValue && p.ConfFimData.HasValue)
            {
                bool incluiConf = !filtrarPorPessoa ||
                                  string.Equals(p.ConfUser, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase);

                if (incluiConf)
                {
                    confTotais++;
                    double minPausaConf = p.HistoricoPausas
                        .Where(h => h.Etapa.Contains("Confer", StringComparison.OrdinalIgnoreCase))
                        .Sum(h => Math.Max(0, ((h.DataHoraRetomada ?? DateTime.Now) - h.DataHoraPausa).TotalMinutes));

                    double minConfLiquido = Math.Max(0, minConf - minPausaConf);
                    if (minConfLiquido <= slaConferenciaMin)
                        confNoPrazo++;
                }
            }

            // Acurácia geral de pedidos finalizados (Expedidos)
            if (p.Status.Equals("Expedido", StringComparison.OrdinalIgnoreCase))
            {
                if (tempoLiquido <= slaPedidoTotalMin)
                    finalizadosNoPrazo++;
                else
                    finalizadosForaPrazo++;
            }
        }

        double tempoMedio = temposPedidos.Any() ? temposPedidos.Average() : 0;
        double acuraciaConf = confTotais > 0 ? Math.Round((double)confNoPrazo / confTotais * 100.0, 1) : 100.0;

        int totalFinalizados = finalizadosNoPrazo + finalizadosForaPrazo;
        double acuraciaTotalPedidos = totalFinalizados > 0
            ? Math.Round((double)finalizadosNoPrazo / totalFinalizados * 100.0, 1)
            : 100.0;

        // Pausas no escopo
        var pausasEscopo = pedidosEscopo
            .SelectMany(p => p.HistoricoPausas)
            .Where(h => !filtrarPorPessoa ||
                        string.Equals(h.OperadorPausa, funcionarioSelecionado, StringComparison.OrdinalIgnoreCase))
            .ToList();

        int qtdPausas = pausasEscopo.Count;
        double tempoTotalPausado = pausasEscopo.Sum(h =>
            Math.Max(0, ((h.DataHoraRetomada ?? DateTime.Now) - h.DataHoraPausa).TotalMinutes));

        // Etapas finalizadas por funcionário (usa todos os pedidos para comparar equipe ou filtra se selecionado)
        var listaFuncionariosGrafico = filtrarPorPessoa
            ? funcionarios.Where(f => f.Equals(funcionarioSelecionado, StringComparison.OrdinalIgnoreCase)).ToList()
            : funcionarios;

        var etapasPorFunc = listaFuncionariosGrafico.Select(func => new EtapasFuncionarioItem
        {
            NomeFuncionario = func,
            SeparacoesFinalizadas = todosPedidos.Count(p =>
                p.SepFimData.HasValue && string.Equals(p.SepUser, func, StringComparison.OrdinalIgnoreCase)),
            ConferenciasFinalizadas = todosPedidos.Count(p =>
                p.ConfFimData.HasValue && string.Equals(p.ConfUser, func, StringComparison.OrdinalIgnoreCase)),
            ExpedicoesFinalizadas = todosPedidos.Count(p =>
                p.ExpFimData.HasValue && string.Equals(p.ExpUser, func, StringComparison.OrdinalIgnoreCase))
        }).ToList();

        // Erros registrados em relação ao limite máximo de 10
        int errosTotais = pedidosEscopo.Sum(p => p.ErrosRegistrados > 0 ? p.ErrosRegistrados : (p.EmEspera ? 1 : 0));

        return new AdmDashboardDto
        {
            FuncionariosDisponiveis = funcionarios,
            EscopoSelecionado = filtrarPorPessoa ? funcionarioSelecionado : "Todos",
            PedidosRecebidos = recebidos,
            PedidosEnviados = enviados,
            TempoMedioPorPedidoMin = Math.Round(tempoMedio, 1),
            ErrosEvitados = errosEvitados,
            AcuraciaConferenciaPercentual = acuraciaConf,
            ConferenciasNoPrazo = confNoPrazo,
            ConferenciasTotais = confTotais,
            QuantidadePausas = qtdPausas,
            TempoTotalPausadoMin = Math.Round(tempoTotalPausado, 1),
            EtapasPorFuncionario = etapasPorFunc,
            ErrosTotais = errosTotais,
            LimiteMaximoErros = 10,
            MetaAcuraciaPercentual = 98.0,
            AcuraciaTotalPedidosPercentual = acuraciaTotalPedidos,
            PedidosFinalizadosNoPrazo = finalizadosNoPrazo,
            PedidosFinalizadosForaPrazo = finalizadosForaPrazo
        };
    }
}