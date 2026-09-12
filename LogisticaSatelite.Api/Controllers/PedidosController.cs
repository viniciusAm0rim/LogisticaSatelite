using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PedidosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos()
        {
            var listapedidos = await _context.Pedidos
                .Include(p => p.Itens)
                .ToListAsync();

            return Ok(listapedidos);
        }

        [HttpPost]
        public async Task<ActionResult<Pedido>> CreatePedido([FromBody] Pedido pedido)
        {
            _context.Pedidos.Add(pedido);
            pedido.Status = "Aguardando";
            await _context.SaveChangesAsync();

            return Ok(pedido);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int Id)
        {
            var pedido = await _context.Pedidos
             .Include(p => p.Itens)
             .FirstOrDefaultAsync(p => p.Id == Id);

            if (pedido == null)
            {
                return NotFound();
            }
            return Ok(pedido);
        }

        [HttpPatch("{id}/separar")]
        public async Task<ActionResult<Pedido>> IniciarSeparacao(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status != "Aguardando")
            {
                throw new ExceptionTemplate("O pedido não está disponível para separação.");
            }

            pedido.Status = "Em Separaçao";
            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/AguardandoConfe")]
        public async Task<ActionResult<Pedido>> TerminoSeparacao(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status != "Em Separaçao")
            {
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");
            }

            pedido.Status = "Aguardando Conferencia";
            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/EmConferencia")]
        public async Task<ActionResult<Pedido>> InicioConferencia(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status != "Aguardando Conferencia")
            {
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");
            }

            pedido.Status = "Em Conferencia";
            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/ConcluirConferencia")]
        public async Task<ActionResult<Pedido>> ConcluirConferencia(int id, [FromBody] int volumes)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status != "Em Conferencia")
            {
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");
            }

            // Atualiza para o status correto e salva a quantidade de volumes informada no app
            pedido.Status = "Aguardando Expediçao";
            pedido.Volumes = volumes;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/AguardandoExped")]
        public async Task<ActionResult<Pedido>> AguardandoExped(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status != "Em Conferencia")
            {
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");
            }

            pedido.Status = "Aguardando Expediçao";
            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/Expedido")]
        public async Task<ActionResult<Pedido>> Expedido(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
            {
                return NotFound();
            }

            if (pedido.Status != "Aguardando Expediçao")
            {
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");
            }

            pedido.Status = "Expedido";
            await _context.SaveChangesAsync();
            return Ok(pedido);
        }
    }
}