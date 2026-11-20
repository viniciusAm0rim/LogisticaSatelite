using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CotacoesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CotacoesController(AppDbContext context)
        {
            _context = context;
        }

        // Lista todas as cotacoes (mais recentes primeiro) ou filtra por status
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cotacao>>> GetCotacoes([FromQuery] string? status)
        {
            var query = _context.Cotacoes
                .Include(c => c.Itens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status.ToLower() == status.ToLower());
            }

            var lista = await query
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return Ok(lista);
        }

        // Busca uma cotacao especifica por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Cotacao>> GetCotacao(int id)
        {
            var cotacao = await _context.Cotacoes
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cotacao == null)
                return NotFound(new { message = $"Cotação #{id} não encontrada." });

            return Ok(cotacao);
        }

        // Busca rapida de produtos no catalogo para o comercial montar a cotacao
        [HttpGet("produtos")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarProdutos([FromQuery] string? termo)
        {
            var query = _context.Produtos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                string t = termo.Trim().ToLower();
                query = query.Where(p =>
                    p.CodItem.ToLower().Contains(t) ||
                    p.Nome.ToLower().Contains(t));
            }

            var produtos = await query
                .OrderBy(p => p.Nome)
                .Take(30)
                .Select(p => new
                {
                    p.Id,
                    p.CodItem,
                    p.Nome
                })
                .ToListAsync();

            return Ok(produtos);
        }

        // Cria uma nova solicitacao de cotacao (Comercial -> Armazem)
        [HttpPost]
        public async Task<ActionResult<Cotacao>> CriarCotacao([FromBody] CriarCotacaoDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Cliente))
                return BadRequest(new { message = "O nome do cliente é obrigatório." });

            if (dto.Itens == null || !dto.Itens.Any())
                return BadRequest(new { message = "Adicione pelo menos um item à cotação." });

            var novaCotacao = new Cotacao
            {
                Cliente = dto.Cliente.Trim(),
                Observacao = dto.Observacao?.Trim(),
                Solicitante = string.IsNullOrWhiteSpace(dto.Solicitante) ? "Comercial" : dto.Solicitante.Trim(),
                DataSolicitacao = DateTime.Now,
                Status = "Pendente"
            };

            foreach (var item in dto.Itens)
            {
                if (item.Quantidade <= 0) continue;

                string nomeItem = item.Nome?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(nomeItem) && !string.IsNullOrWhiteSpace(item.CodItem))
                {
                    var prodDb = await _context.Produtos.FirstOrDefaultAsync(p => p.CodItem == item.CodItem.Trim());
                    nomeItem = prodDb?.Nome ?? item.CodItem.Trim();
                }

                novaCotacao.Itens.Add(new ItemCotacao
                {
                    CodItem = item.CodItem?.Trim() ?? "MANUAL",
                    Nome = string.IsNullOrWhiteSpace(nomeItem) ? "Item sem descrição" : nomeItem,
                    Quantidade = item.Quantidade
                });
            }

            if (!novaCotacao.Itens.Any())
                return BadRequest(new { message = "Informe quantidades maiores que zero." });

            _context.Cotacoes.Add(novaCotacao);
            await _context.SaveChangesAsync();

            return Ok(novaCotacao);
        }

        // Responde a cotacao com volumes, peso e dimensoes (Armazem -> Comercial)
        [HttpPatch("{id}/responder")]
        [HttpPost("{id}/responder")]
        public async Task<ActionResult<Cotacao>> ResponderCotacao(int id, [FromBody] ResponderCotacaoDTO dto)
        {
            var cotacao = await _context.Cotacoes
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cotacao == null)
                return NotFound(new { message = $"Cotação #{id} não encontrada." });

            if (dto.Volumes <= 0)
                return BadRequest(new { message = "Informe a quantidade de volumes (maior que zero)." });

            if (dto.PesoTotalKg <= 0)
                return BadRequest(new { message = "Informe o peso total em kg (maior que zero)." });

            if (string.IsNullOrWhiteSpace(dto.Dimensoes))
                return BadRequest(new { message = "Informe as dimensões/cubagem." });

            cotacao.Volumes = dto.Volumes;
            cotacao.PesoTotalKg = Math.Round(dto.PesoTotalKg, 2);
            cotacao.Dimensoes = dto.Dimensoes.Trim();
            cotacao.RespondidoPor = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario.Trim();
            cotacao.DataResposta = DateTime.Now;
            cotacao.Status = "Respondida";

            await _context.SaveChangesAsync();
            return Ok(cotacao);
        }
    }

    public class CriarCotacaoDTO
    {
        public string Cliente { get; set; } = string.Empty;
        public string? Observacao { get; set; }
        public string? Solicitante { get; set; }
        public List<ItemCotacaoInputDTO> Itens { get; set; } = new();
    }

    public class ItemCotacaoInputDTO
    {
        public string CodItem { get; set; } = string.Empty;
        public string? Nome { get; set; }
        public int Quantidade { get; set; }
    }

    public class ResponderCotacaoDTO
    {
        public int Volumes { get; set; }
        public double PesoTotalKg { get; set; }
        public string Dimensoes { get; set; } = string.Empty;
        public string? Usuario { get; set; }
    }
}