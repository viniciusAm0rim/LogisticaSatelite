using LogisticaSatelite.Api.Data;
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

        [HttpGet("exportar-csv")]
        public async Task<IActionResult> ExportarCsv([FromQuery] string? operador)
        {
            var query = _context.Pedidos.AsQueryable();

            if (!string.IsNullOrEmpty(operador) && operador != "Todos")
            {
                query = query.Where(p => p.SepUser == operador || p.ConfUser == operador || p.ExpUser == operador);
            }

            var pedidos = await query.ToListAsync();
            var culturaBr = new CultureInfo("pt-BR");

            var csv = new System.Text.StringBuilder();

            // Cabeçalho atualizado (sem Hora Inicio Expedicao)
            csv.AppendLine("ID Pedido;Cliente;Status;Sep User;Conf User;Exp User;Hora Inicio Separacao;Hora Fim Separacao;Hora Inicio Conferencia;Hora Fim Conferencia;Data Expedicao;Erros Evitados;Tempo Medio (min)");

            foreach (var p in pedidos)
            {
                double tSep = (p.SepFimData.HasValue && p.SepInicioData.HasValue)
                    ? (p.SepFimData.Value - p.SepInicioData.Value).TotalMinutes
                    : 0;

                double tConf = (p.ConfFimData.HasValue && p.ConfInicioData.HasValue)
                    ? (p.ConfFimData.Value - p.ConfInicioData.Value).TotalMinutes
                    : 0;

                // Expedição usa apenas o momento em que foi concluída
                double tExp = 0;

                double tempoMedioCalculado = (tSep + tConf) / 2.0; // Média baseada nas etapas com início e fim

                string sepInicio = p.SepInicioData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string sepFim = p.SepFimData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string confInicio = p.ConfInicioData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string confFim = p.ConfFimData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-";
                string expFim = p.ExpFimData?.ToString("yyyy-MM-dd HH:mm:ss", culturaBr) ?? "-"; // Apenas a data de conclusão

                string tempoMedioFormatado = tempoMedioCalculado.ToString("N2", culturaBr);

                csv.AppendLine($"{p.Id};{p.Destinatario};{p.Status};{p.SepUser ?? "-"};{p.ConfUser ?? "-"};{p.ExpUser ?? "-"};{sepInicio};{sepFim};{confInicio};{confFim};{expFim};{p.ErrosEvitados};{tempoMedioFormatado}");
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}