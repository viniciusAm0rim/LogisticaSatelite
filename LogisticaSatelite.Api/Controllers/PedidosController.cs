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
                .Include(p => p.VolumesPedidos)
                .ToListAsync();

            return Ok(listapedidos);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int Id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.VolumesPedidos)
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
                    Peso = 0.0,
                    ErrosEvitados = 0
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

                var pedidoCriado = await _context.Pedidos
                    .Include(p => p.Itens).ThenInclude(i => i.Produto)
                    .Include(p => p.VolumesPedidos)
                    .FirstOrDefaultAsync(p => p.Id == novoPedido.Id);
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
            pedido.SepInicioData = DateTime.Now;
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
            pedido.SepFimData = DateTime.Now;

            // Soma/acumula os erros reportados na separação
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;

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
            pedido.ConfInicioData = DateTime.Now;
            if (!string.IsNullOrEmpty(dto.Usuario)) pedido.LockedBy = dto.Usuario;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/ConcluirConferencia")]
        public async Task<ActionResult<Pedido>> ConcluirConferencia(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .Include(p => p.VolumesPedidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (pedido.Status != "Em Conferencia")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Aguardando Expediçao";
            pedido.Volumes = dto.Volumes;
            pedido.ConfUser = dto.Usuario;
            pedido.ConfFimData = DateTime.Now;

            // Soma/acumula os erros reportados na conferência
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;

            _context.VolumesPedidos.RemoveRange(pedido.VolumesPedidos);
            pedido.VolumesPedidos.Clear();

            int clienteId = 1;
            if (!string.IsNullOrEmpty(pedido.Destinatario))
            {
                var clienteDb = await _context.Clientes.FirstOrDefaultAsync(c => c.Nome == pedido.Destinatario);
                if (clienteDb != null) clienteId = clienteDb.Id;
            }

            for (int i = 1; i <= dto.Volumes; i++)
            {
                string codigoGerado = $"c{clienteId}p{pedido.Id}v{i}";
                pedido.VolumesPedidos.Add(new VolumePedido
                {
                    PedidoId = pedido.Id,
                    CodigoVolume = codigoGerado,
                    BipadoExpedicao = false
                });
            }

            await _context.SaveChangesAsync();

            var pedidoAtualizado = await _context.Pedidos
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .Include(p => p.VolumesPedidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            return Ok(pedidoAtualizado);
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
            pedido.ExpFimData = DateTime.Now;

            // Soma/acumula os erros reportados na expedição
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/reverter")]
        public async Task<ActionResult<Pedido>> ReverterEtapa(int id)
        {
            // Precisamos do Include para limpar os itens ou volumes ao cancelar
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .Include(p => p.VolumesPedidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (pedido.Status == "Em Separaçao")
            {
                pedido.Status = "Aguardando";
                pedido.SepInicioData = null;
                pedido.LockedBy = null;

                // Zera o progresso da separação nos itens
                foreach (var item in pedido.Itens)
                {
                    item.ScansSep = 0;
                }
            }
            else if (pedido.Status == "Em Conferencia")
            {
                pedido.Status = "Aguardando Conferencia";
                pedido.ConfInicioData = null;
                pedido.LockedBy = null;

                // Zera o progresso da conferência nos itens
                foreach (var item in pedido.Itens)
                {
                    item.ScansConf = 0;
                }
            }
            else if (pedido.Status == "Em Expediçao") // Caso futuramente crie o status "Em Expedição" antes da baixa final
            {
                pedido.Status = "Aguardando Expediçao";
                pedido.LockedBy = null;

                // Zera o progresso da expedição nos volumes
                foreach (var volume in pedido.VolumesPedidos)
                {
                    volume.BipadoExpedicao = false;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }
    }

    public class EtapaRequestDTO
    {
        public string? Usuario { get; set; }
        public int Volumes { get; set; }
        public int ErrosEvitados { get; set; }
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