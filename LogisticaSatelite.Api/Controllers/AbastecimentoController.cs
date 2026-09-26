using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbastecimentoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AbastecimentoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("recomendacoes")]
        public async Task<ActionResult<IEnumerable<Produto>>> GetRecomendacoesAbastecimento()
        {
            var produtosParaAbastecer = await _context.Produtos
                .Where(p => p.QtdPicking <= p.QtdMinimaPicking)
                .OrderBy(p => p.QtdPicking) // Ordena do menor estoque para o maior
                .ToListAsync();

            return Ok(produtosParaAbastecer);
        }

        // Confirma a reposição incrementando o picking do produto e baixando do pulmão
        [HttpPost("confirmar")]
        public async Task<IActionResult> ConfirmarAbastecimento([FromBody] AbastecimentoDTO dto)
        {
            var produto = await _context.Produtos.FindAsync(dto.ProdutoId);
            if (produto == null)
            {
                return NotFound("Produto não encontrado.");
            }

            // Simula a entrada da caixa no picking (ex: adiciona o equivalente a 1 caixa ou o lote pendente)
            int quantidadeAdicionada = produto.UnidadepCx > 0 ? produto.UnidadepCx : 10;
            produto.QtdPicking += quantidadeAdicionada;

            await _context.SaveChangesAsync();
            return Ok(new { Mensagem = "Abastecimento confirmado com sucesso!" });
        }
    }

    public class AbastecimentoDTO
    {
        public int ProdutoId { get; set; }
        public string MatriculaUsuario { get; set; } = string.Empty;
    }
}