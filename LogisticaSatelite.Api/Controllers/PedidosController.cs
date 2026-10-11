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

        // ==========================================
        // LOCK PEDIDO
        // ==========================================
        [HttpPatch("{id}/lock")]
        public async Task<ActionResult<Pedido>> LockPedido(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (!string.IsNullOrEmpty(dto.Usuario))
            {
                pedido.LockedBy = dto.Usuario;
                await _context.SaveChangesAsync();
            }

            return Ok(pedido);
        }

        // ==========================================
        // GET PEDIDOS (COM FILTRO OPCIONAL DE STATUS)
        // ==========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos([FromQuery] string? status)
        {
            var query = _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.VolumesPedidos)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            }

            var listapedidos = await query.ToListAsync();
            return Ok(listapedidos);
        }

        // ==========================================
        // GET PEDIDO POR ID
        // ==========================================
        [HttpGet("{Id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int Id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.VolumesPedidos)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{Id} não encontrado." });

            return Ok(pedido);
        }

        // ==========================================
        // CRIAR PEDIDO SIMPLIFICADO
        // ==========================================
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

        // ==========================================
        // FLUXO DE SEPARAÇÃO / CONFERÊNCIA / EXPEDIÇÃO
        // ==========================================
        [HttpPatch("{id}/separar")]
        public async Task<ActionResult<Pedido>> IniciarSeparacao(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Aguardando")
                throw new ExceptionTemplate("O pedido não está disponível para separação.");

            pedido.Status = "Em Separação";
            pedido.SepInicioData = DateTime.Now;

            if (!string.IsNullOrEmpty(dto.Usuario)) pedido.LockedBy = dto.Usuario;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/AguardandoConfe")]
        public async Task<ActionResult<Pedido>> TerminoSeparacao(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Em Separação")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Aguardando Conferencia";
            pedido.SepUser = dto.Usuario;
            pedido.SepFimData = DateTime.Now;
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/EmConferencia")]
        public async Task<ActionResult<Pedido>> InicioConferencia(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

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

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Em Conferencia")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Aguardando Expediçao";
            pedido.Volumes = dto.Volumes;
            pedido.ConfUser = dto.Usuario;
            pedido.ConfFimData = DateTime.Now;
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
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Aguardando Expediçao")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Expedido";
            pedido.ExpUser = dto.Usuario;
            pedido.ExpFimData = DateTime.Now;
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        [HttpPatch("{id}/reverter")]
        public async Task<ActionResult<Pedido>> ReverterEtapa(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .Include(p => p.VolumesPedidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status == "Em Separação")
            {
                pedido.Status = "Aguardando";
                pedido.SepInicioData = null;
                pedido.LockedBy = null;
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
                foreach (var item in pedido.Itens)
                {
                    item.ScansConf = 0;
                }
            }
            else if (pedido.Status == "Em Expediçao")
            {
                pedido.Status = "Aguardando Expediçao";
                pedido.LockedBy = null;
                foreach (var volume in pedido.VolumesPedidos)
                {
                    volume.BipadoExpedicao = false;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // ==========================================
        // 1. SOLICITAÇÃO DE ESTIMATIVA (WebComercial -> API)
        // ==========================================
        [HttpPost("{id}/solicitar-estimativa")]
        [HttpPatch("{id}/solicitar-estimativa")]
        [HttpPut("{id}/solicitar-estimativa")]
        [HttpPost("/api/Pedido/{id}/solicitar-estimativa")]
        [HttpPatch("/api/Pedido/{id}/solicitar-estimativa")]
        public async Task<IActionResult> SolicitarEstimativa(int id)
        {
            Console.WriteLine($"[API] Solicitação de estimativa recebida para o Pedido ID: {id}");

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound(new { message = $"Pedido #{id} não foi encontrado na base de dados SQLite." });
            }

            pedido.EstimativaSolicitada = true;
            pedido.SolicitanteEstimativa = "Comercial";

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // ==========================================
        // 2. RESPOSTA DE ESTIMATIVA (SpringApp -> API)
        // ==========================================
        [HttpPost("{id}/estimativa")]
        [HttpPatch("{id}/estimativa")]
        [HttpPut("{id}/estimativa")]
        [HttpPost("{id}/responder-estimativa")]
        [HttpPatch("{id}/responder-estimativa")]
        [HttpPut("{id}/responder-estimativa")]
        [HttpPost("{id}/enviar-estimativa")]
        [HttpPatch("{id}/enviar-estimativa")]
        [HttpPut("{id}/enviar-estimativa")]
        [HttpPost("estimativa")]
        [HttpPatch("estimativa")]
        [HttpPut("estimativa")]
        [HttpPost("responder-estimativa")]
        [HttpPatch("responder-estimativa")]
        [HttpPut("responder-estimativa")]
        [HttpPost("enviar-estimativa")]
        [HttpPatch("enviar-estimativa")]
        [HttpPut("enviar-estimativa")]
        [HttpPost("/api/Pedido/{id}/estimativa")]
        [HttpPost("/api/Pedido/estimativa")]
        public async Task<IActionResult> ResponderEstimativa(
            [FromRoute] int id,
            [FromBody] ResponderEstimativaDTO? dtoBody,
            [FromQuery] int? idQuery)
        {
            int pedidoIdFinal = id > 0 ? id : (dtoBody?.Id ?? dtoBody?.PedidoId ?? idQuery ?? 0);

            string dataEstimada = !string.IsNullOrWhiteSpace(dtoBody?.DataHoraEstimada)
                ? dtoBody.DataHoraEstimada
                : (!string.IsNullOrWhiteSpace(dtoBody?.DataHora)
                    ? dtoBody.DataHora
                    : dtoBody?.DataHoraEstimadaString ?? string.Empty);

            Console.WriteLine($"[API] Resposta de estimativa recebida para Pedido ID: {pedidoIdFinal} | Valor: '{dataEstimada}'");

            if (pedidoIdFinal <= 0)
            {
                return BadRequest(new { message = "O ID do pedido não foi fornecido na URL nem no corpo da requisição." });
            }

            var pedido = await _context.Pedidos.FindAsync(pedidoIdFinal);
            if (pedido == null)
            {
                return NotFound(new { message = $"Pedido #{pedidoIdFinal} não foi encontrado na base de dados SQLite." });
            }

            if (string.IsNullOrWhiteSpace(dataEstimada))
            {
                return BadRequest(new { message = "A data/hora estimada é obrigatória." });
            }

            pedido.DataHoraEstimada = dataEstimada;
            pedido.EstimativaSolicitada = false; // Conclui o ciclo de solicitação

            await _context.SaveChangesAsync();

            Console.WriteLine($"[API] Estimativa gravada com sucesso para o Pedido #{pedidoIdFinal}!");
            return Ok(pedido);
        }
    }

    // ==========================================
    // DTOs AUXILIARES
    // ==========================================
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

    public class ResponderEstimativaDTO
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public string? DataHora { get; set; }
        public string? DataHoraEstimada { get; set; }
        public string? DataHoraEstimadaString { get; set; }
    }
}