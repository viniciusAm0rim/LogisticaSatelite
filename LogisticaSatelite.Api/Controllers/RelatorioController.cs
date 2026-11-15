using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RelatorioController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RelatorioController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("painel-gerencial")]
        public async Task<IActionResult> GetPainelGerencial(
            [FromQuery] string periodo = "Semana",
            [FromQuery] string operador = "Todos",
            [FromQuery] string status = "Todos")
        {
            var query = _context.Pedidos
                .Include(p => p.HistoricoPausas)
                .AsQueryable();

            DateTime dataReferencia = DateTime.Now.Date;
            DateTime dataCorte = periodo switch
            {
                "Semana" => dataReferencia.AddDays(-6),
                "Mes" => dataReferencia.AddDays(-29),
                "Trimestre" => dataReferencia.AddDays(-89),
                _ => dataReferencia.AddDays(-6)
            };

            query = query.Where(p => (p.ExpFimData ?? p.SepInicioData ?? dataReferencia) >= dataCorte);

            if (!string.IsNullOrEmpty(operador) && operador != "Todos")
            {
                query = query.Where(p =>
                    p.SepUser == operador ||
                    p.ConfUser == operador ||
                    p.ExpUser == operador ||
                    p.HistoricoPausas.Any(h => h.OperadorPausa == operador || h.OperadorRetomada == operador));
            }

            if (!string.IsNullOrEmpty(status) && status != "Todos")
            {
                if (status.Equals("Em Espera", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => p.EmEspera);
                }
                else
                {
                    query = query.Where(p => p.Status.ToLower() == status.ToLower());
                }
            }

            var pedidosDb = await query.ToListAsync();
            var resultado = new List<PedidoFatoDto>();

            foreach (var p in pedidosDb)
            {
                double tSep = (p.SepFimData.HasValue && p.SepInicioData.HasValue)
                    ? (p.SepFimData.Value - p.SepInicioData.Value).TotalMinutes
                    : 0;

                double tConf = (p.ConfFimData.HasValue && p.ConfInicioData.HasValue)
                    ? (p.ConfFimData.Value - p.ConfInicioData.Value).TotalMinutes
                    : 0;

                int qtdPausas = p.HistoricoPausas?.Count ?? 0;
                double minutosPausados = 0;

                if (p.HistoricoPausas != null)
                {
                    foreach (var h in p.HistoricoPausas)
                    {
                        DateTime fimRef = h.DataHoraRetomada ?? DateTime.Now;
                        minutosPausados += Math.Max(0, (fimRef - h.DataHoraPausa).TotalMinutes);
                    }
                }

                double tempoLiquido = Math.Max(0, (tSep + tConf) - minutosPausados);
                double diasConferencia = tConf > 0 ? Math.Round(tConf / 1440.0 + 1.0, 1) : 1.0;

                resultado.Add(new PedidoFatoDto
                {
                    IdPedido = p.Id,
                    DataOperacao = p.ExpFimData ?? p.SepInicioData ?? DateTime.Now,
                    Cliente = p.Destinatario ?? "Cliente Não Informado",
                    Modal = string.IsNullOrWhiteSpace(p.Modal) ? "Padrão" : p.Modal,
                    Status = p.EmEspera ? "Em Espera" : p.Status,
                    Volumes = p.Volumes > 0 ? p.Volumes : 1,
                    EmEspera = p.EmEspera,
                    SepUser = p.SepUser,
                    ConfUser = p.ConfUser,
                    ExpUser = p.ExpUser,
                    DiasUteisConferencia = diasConferencia,
                    QtdPausas = qtdPausas,
                    TempoPausadoMin = Math.Round(minutosPausados, 2),
                    TempoLiquidoMin = Math.Round(tempoLiquido, 2),
                    ErrosEvitados = p.ErrosEvitados,
                    ErrosReais = p.EmEspera ? 1 : 0
                });
            }

            return Ok(resultado);
        }

        [HttpGet("exportar-csv")]
        public async Task<IActionResult> ExportarCsv([FromQuery] string? operador)
        {
            var query = _context.Pedidos
                .Include(p => p.HistoricoPausas)
                .AsQueryable();

            if (!string.IsNullOrEmpty(operador) && operador != "Todos")
            {
                query = query.Where(p =>
                    p.SepUser == operador ||
                    p.ConfUser == operador ||
                    p.ExpUser == operador ||
                    p.HistoricoPausas.Any(h => h.OperadorPausa == operador || h.OperadorRetomada == operador));
            }

            var pedidos = await query.ToListAsync();
            var culturaBr = new CultureInfo("pt-BR");
            var csv = new StringBuilder();

            csv.AppendLine("ID Pedido;Cliente;Status;Sep User;Conf User;Exp User;Hora Inicio Separacao;Hora Fim Separacao;Hora Inicio Conferencia;Hora Fim Conferencia;Data Expedicao;Erros Evitados;Qtd Pausas;Tempo Pausado (min);Historico Pausas e Retomadas;Tempo Medio (min)");

            foreach (var p in pedidos)
            {
                double tSep = (p.SepFimData.HasValue && p.SepInicioData.HasValue) ? (p.SepFimData.Value - p.SepInicioData.Value).TotalMinutes : 0;
                double tConf = (p.ConfFimData.HasValue && p.ConfInicioData.HasValue) ? (p.ConfFimData.Value - p.ConfInicioData.Value).TotalMinutes : 0;

                int qtdPausas = p.HistoricoPausas?.Count ?? 0;
                double minutosPausados = 0;
                string detalhesPausas = "-";

                if (qtdPausas > 0 && p.HistoricoPausas != null)
                {
                    var eventosFormatados = new List<string>();
                    foreach (var h in p.HistoricoPausas.OrderBy(x => x.DataHoraPausa))
                    {
                        DateTime fimRef = h.DataHoraRetomada ?? DateTime.Now;
                        minutosPausados += Math.Max(0, (fimRef - h.DataHoraPausa).TotalMinutes);
                        string retomadaStr = h.DataHoraRetomada.HasValue ? $"Retomado {h.DataHoraRetomada.Value:HH:mm:ss} ({h.OperadorRetomada})" : "Ainda em espera";
                        eventosFormatados.Add($"[{h.Etapa}] Pausa {h.DataHoraPausa:HH:mm:ss} ({h.OperadorPausa}) -> {retomadaStr}");
                    }
                    detalhesPausas = string.Join(" | ", eventosFormatados);
                }

                double tempoAtivoTotal = Math.Max(0, (tSep + tConf) - minutosPausados);
                double tempoMedioCalculado = tempoAtivoTotal / 2.0;

                string statusExibicao = p.EmEspera ? $"Em Espera ({p.EtapaPausada})" : p.Status;
                string sepInicio = p.SepInicioData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string sepFim = p.SepFimData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string confInicio = p.ConfInicioData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string confFim = p.ConfFimData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string expFim = p.ExpFimData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";

                csv.AppendLine($"{p.Id};{p.Destinatario};{statusExibicao};{p.SepUser ?? "-"};{p.ConfUser ?? "-"};{p.ExpUser ?? "-"};{sepInicio};{sepFim};{confInicio};{confFim};{expFim};{p.ErrosEvitados};{qtdPausas};{minutosPausados.ToString("N2", culturaBr)};{detalhesPausas};{tempoMedioCalculado.ToString("N2", culturaBr)}");
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}