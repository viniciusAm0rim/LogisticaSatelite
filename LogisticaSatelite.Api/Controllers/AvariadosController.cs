using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvariadosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AvariadosController(AppDbContext context)
        {
            _context = context;
        }

        // Lista todos os registros de produtos avariados (Estoque Interno + Devolucoes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdutoAvariado>>> GetAvariados()
        {
            var lista = await _context.ProdutosAvariados
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return Ok(lista);
        }

        // Operador bipa um produto quebrado/danificado diretamente no estoque e adiciona a lista de avariados
        [HttpPost("registrar-estoque")]
        public async Task<IActionResult> RegistrarAvariaEstoque([FromBody] RegistrarAvariaEstoqueDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CodItem))
                return BadRequest(new { message = "Informe o código (SKU) do produto avariado." });

            if (dto.Quantidade <= 0)
                return BadRequest(new { message = "A quantidade avariada deve ser maior que zero." });

            string codNorm = dto.CodItem.Trim();
            var produtoDb = await _context.Produtos
                .FirstOrDefaultAsync(p => p.CodItem.ToLower() == codNorm.ToLower());

            if (produtoDb == null)
                return NotFound(new { message = $"Produto com SKU '{codNorm}' não encontrado no cadastro." });

            string usuarioOp = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario.Trim();
            string motivo = string.IsNullOrWhiteSpace(dto.MotivoAvaria) ? "Avaria identificada no estoque" : dto.MotivoAvaria.Trim();

            // Desconta do estoque disponivel (QtdPicking) sem deixar negativo
            produtoDb.QtdPicking = Math.Max(0, produtoDb.QtdPicking - dto.Quantidade);

            var registro = new ProdutoAvariado
            {
                CodItem = produtoDb.CodItem,
                Nome = produtoDb.Nome,
                Quantidade = dto.Quantidade,
                Origem = "Estoque Interno",
                DevolucaoId = null,
                PedidoId = null,
                MotivoAvaria = motivo,
                RegistradoPor = usuarioOp,
                DataRegistro = DateTime.Now
            };

            _context.ProdutosAvariados.Add(registro);
            await _context.SaveChangesAsync();

            return Ok(registro);
        }

        // ADM remove/da baixa definitiva em um item avariado (ex.: descarte ou sinistro concluido)
        [HttpDelete("{id}")]
        public async Task<IActionResult> BaixarAvariado(int id)
        {
            var item = await _context.ProdutosAvariados.FindAsync(id);
            if (item == null)
                return NotFound(new { message = $"Registro de avaria #{id} não encontrado." });

            _context.ProdutosAvariados.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Registro de avaria baixado com sucesso." });
        }
    }

    public class RegistrarAvariaEstoqueDTO
    {
        public string CodItem { get; set; } = string.Empty;
        public int Quantidade { get; set; } = 1;
        public string? MotivoAvaria { get; set; }
        public string? Usuario { get; set; }
    }
}