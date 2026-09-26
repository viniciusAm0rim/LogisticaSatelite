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

        // Adicione este método dentro de PedidosController
        [HttpPatch("{id}/lock")]
        public async Task<ActionResult<Pedido>> LockPedido(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Usuario))
            {
                pedido.LockedBy = dto.Usuario;
                await _context.SaveChangesAsync();
            }

            return Ok(pedido);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos()
        {
            var listapedidos = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .ToListAsync();

            return Ok(listapedidos);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int Id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (pedido == null) return NotFound();
            return Ok(pedido);
        }

        [HttpPost]
        public async Task<ActionResult<Pedido>> CreatePedido([FromBody] CriarPedidoSimplificadoDTO dto)
        {
            try
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Codigo == dto.CodCliente);
                if (cliente == null) return BadRequest(new { message = $"Cliente com código '{dto.CodCliente}' não encontrado." });

                string modalTexto = dto.Modal switch
                {
                    1 => "Loja",
                    2 => "Retira",
                    3 => "Transportadora",
                    _ => "Desconhecido"
                };

                if (modalTexto == "Desconhecido") return BadRequest(new { message = "Modal inválido." });

                string destinatarioFinal = cliente.Nome;
                string modalFinal = modalTexto;

                if (dto.Modal == 3 && !string.IsNullOrEmpty(dto.CodigoTransportadora))
                {
                    var transportadora = await _context.Transportadoras.FirstOrDefaultAsync(t => t.Codigo == dto.CodigoTransportadora);
                    string nomeTransp = transportadora != null ? transportadora.Nome : dto.CodigoTransportadora;
                    modalFinal = $"Transportadora ({nomeTransp})";
                }

                var novoPedido = new Pedido
                {
                    Destinatario = destinatarioFinal,
                    Modal = modalFinal,
                    Status = "Aguardando",
                    Volumes = 0,
                    Peso = 0.0
                };

                foreach (var itemInput in dto.Itens)
                {
                    var produtoDb = await _context.Produtos.FirstOrDefaultAsync(p => p.CodItem == itemInput.CodItem);
                    if (produtoDb == null) return BadRequest(new { message = $"Produto '{itemInput.CodItem}' não encontrado." });

                    if (produtoDb.QtdPicking < itemInput.Quantidade)
                    {
                        produtoDb.QtdCxAbaste = (int)Math.Ceiling((double)(itemInput.Quantidade - produtoDb.QtdPicking) / (produtoDb.UnidadepCx > 0 ? produtoDb.UnidadepCx : 1));
                    }

                    novoPedido.Itens.Add(new ItemPedido
                    {
                        ProdutoId = produtoDb.Id,
                        CodItem = produtoDb.CodItem,
                        Nome = produtoDb.Nome,
                        LocalPicking = produtoDb.LocalPicking,
                        LocalPulmao = produtoDb.LocalPulmao,
                        Quantidade = itemInput.Quantidade,
                        ScansSep = 0,
                        ScansConf = 0,
                        ScansExp = 0
                    });
                }

                _context.Pedidos.Add(novoPedido);
                await _context.SaveChangesAsync();

                var pedidoCriado = await _context.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Produto).FirstOrDefaultAsync(p => p.Id == novoPedido.Id);
                return Ok(pedidoCriado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPatch("{id}/separar")]
        public async Task<ActionResult<Pedido>> IniciarSeparacao(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Status != "Aguardando")
                throw new ExceptionTemplate("O pedido não está disponível para separação.");

            pedido.Status = "Em Separaçao";

            // Grava quem INICIOU a separação no LockedBy
            if (!string.IsNullOrEmpty(dto.Usuario)) pedido.LockedBy = dto.Usuario;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/AguardandoConfe")]
        public async Task<ActionResult<Pedido>> TerminoSeparacao(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Status != "Em Separaçao")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Aguardando Conferencia";

            pedido.SepUser = dto.Usuario;
            pedido.SepData = DateTime.Now;
            pedido.LockedBy = null; // Libera o lock

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/EmConferencia")]
        public async Task<ActionResult<Pedido>> InicioConferencia(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Status != "Aguardando Conferencia")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Em Conferencia";

            if (!string.IsNullOrEmpty(dto.Usuario)) pedido.LockedBy = dto.Usuario;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/ConcluirConferencia")]
        public async Task<ActionResult<Pedido>> ConcluirConferencia(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Status != "Em Conferencia")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Aguardando Expediçao";
            pedido.Volumes = dto.Volumes;

            pedido.ConfUser = dto.Usuario;
            pedido.ConfData = DateTime.Now;
            pedido.LockedBy = null;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/AguardandoExped")]
        public async Task<ActionResult<Pedido>> AguardandoExped(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.Status = "Aguardando Expediçao";
            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/Expedido")]
        public async Task<ActionResult<Pedido>> Expedido(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Status != "Aguardando Expediçao")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Expedido";

            pedido.ExpUser = dto.Usuario;
            pedido.ExpData = DateTime.Now;
            pedido.LockedBy = null;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/reverter")]
        public async Task<ActionResult<Pedido>> ReverterEtapa(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Status == "Em Separaçao")
            {
                pedido.Status = "Aguardando";
                pedido.LockedBy = null;
            }
            else if (pedido.Status == "Em Conferencia")
            {
                pedido.Status = "Aguardando Conferencia";
                pedido.LockedBy = null;
            }

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }
    }

    public class EtapaRequestDTO
    {
        public string? Usuario { get; set; }
        public int Volumes { get; set; }
    }

    public class CriarPedidoSimplificadoDTO
    {
        public string CodCliente { get; set; } = string.Empty;
        public int Modal { get; set; }
        public string? CodigoTransportadora { get; set; }
        public List<ItemPedidoInputDTO> Itens { get; set; } = new();
    }

    public class ItemPedidoInputDTO
    {
        public string CodItem { get; set; } = string.Empty;
        public int Quantidade { get; set; }
    }
}