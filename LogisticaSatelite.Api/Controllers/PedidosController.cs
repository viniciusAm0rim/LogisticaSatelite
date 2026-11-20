using System.Globalization;
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

        // Registra qual operador assumiu o pedido
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

        // Lista todos os pedidos ou filtra por status
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos([FromQuery] string? status)
        {
            var query = _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.VolumesPedidos)
                .Include(p => p.HistoricoPausas)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            }

            var listapedidos = await query.ToListAsync();
            return Ok(listapedidos);
        }

        // Busca os dados completos de um pedido pelo ID
        [HttpGet("{Id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int Id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.VolumesPedidos)
                .Include(p => p.HistoricoPausas)
                .FirstOrDefaultAsync(p => p.Id == Id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{Id} não encontrado." });

            return Ok(pedido);
        }

        // Cria um novo pedido com itens e baixa estoque do picking
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

                    // Rota definida diretamente no banco de dados (0 = Comum | 1 = Area Tecnica)
                    novoPedido.Itens.Add(new ItemPedido
                    {
                        ProdutoId = produtoDb.Id,
                        CodItem = produtoDb.CodItem,
                        Nome = produtoDb.Nome,
                        LocalPicking = produtoDb.LocalPicking,
                        LocalPulmao = produtoDb.LocalPulmao,
                        TipoRota = produtoDb.TipoRota,
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

        // Inicia a separacao do pedido (permite entrada paralela da Separacao Normal e Area Tecnica)
        [HttpPatch("{id}/separar")]
        public async Task<ActionResult<Pedido>> IniciarSeparacao(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Aguardando" && pedido.Status != "Em Separação" && pedido.Status != "Em Separacao")
                throw new ExceptionTemplate("O pedido não está disponível para separação.");

            pedido.Status = "Em Separação";
            pedido.SepInicioData ??= DateTime.Now;
            pedido.EmEspera = false;
            pedido.EtapaPausada = null;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;

            if (!string.IsNullOrEmpty(dto.Usuario)) pedido.LockedBy = dto.Usuario;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Conclui a separacao apenas da rota informada (0 = Normal ou 1 = Area Tecnica).
        // O pedido SO avanca para "Aguardando Conferencia" quando TODOS os itens (rota 0 e rota 1) estiverem bipados!
        [HttpPatch("{id}/AguardandoConfe")]
        public async Task<ActionResult<Pedido>> TerminoSeparacao(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Em Separação" && pedido.Status != "Em Separacao" && pedido.Status != "Aguardando")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            // Sincroniza TipoRota com o cadastro atual do Produto caso o item tenha sido criado antes
            foreach (var item in pedido.Itens)
            {
                if (item.Produto != null && item.TipoRota != item.Produto.TipoRota)
                {
                    item.TipoRota = item.Produto.TipoRota;
                }
            }

            // Filtra estritamente os itens da rota que esta sendo finalizada nesta requisicao
            var itensDaRotaAtual = dto.TipoRota.HasValue
                ? pedido.Itens.Where(i => i.TipoRota == dto.TipoRota.Value).ToList()
                : pedido.Itens.ToList();

            if (dto.Itens != null && dto.Itens.Any())
            {
                foreach (var itemDto in dto.Itens)
                {
                    var itemDb = itensDaRotaAtual.FirstOrDefault(i =>
                        (itemDto.Id > 0 && i.Id == itemDto.Id) ||
                        (!string.IsNullOrEmpty(itemDto.CodItem) && i.CodItem.Equals(itemDto.CodItem, StringComparison.OrdinalIgnoreCase)));

                    if (itemDb != null)
                    {
                        itemDb.ScansSep = Math.Min(itemDb.Quantidade, Math.Max(itemDb.ScansSep, itemDto.ScansSep));
                    }
                }
            }
            else
            {
                // Se nenhum item individual foi enviado no payload, conclui apenas os itens desta rota
                foreach (var itemDb in itensDaRotaAtual)
                {
                    itemDb.ScansSep = itemDb.Quantidade;
                }
            }

            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;
            pedido.EmEspera = false;
            pedido.EtapaPausada = null;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;

            if (!string.IsNullOrWhiteSpace(dto.Usuario))
            {
                if (string.IsNullOrWhiteSpace(pedido.SepUser))
                    pedido.SepUser = dto.Usuario;
                else if (!pedido.SepUser.Contains(dto.Usuario))
                    pedido.SepUser = $"{pedido.SepUser} / {dto.Usuario}";
            }

            // REGRA CRITICA: Verifica TODOS os itens do pedido (tanto TipoRota == 0 quanto TipoRota == 1)
            bool todosItensSeparados = pedido.Itens.Any() && pedido.Itens.All(i => i.ScansSep >= i.Quantidade);

            if (todosItensSeparados)
            {
                pedido.Status = "Aguardando Conferencia";
                pedido.SepFimData = DateTime.Now;
            }
            else
            {
                pedido.Status = "Em Separação";
            }

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Inicia a conferencia do pedido
        [HttpPatch("{id}/EmConferencia")]
        public async Task<ActionResult<Pedido>> InicioConferencia(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Aguardando Conferencia")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            if (pedido.Itens.Any(i => i.ScansSep < i.Quantidade))
                throw new ExceptionTemplate("Ainda existem itens pendentes de separação (Normal ou Área Técnica).");

            pedido.Status = "Em Conferencia";
            pedido.ConfInicioData ??= DateTime.Now;
            pedido.EmEspera = false;
            pedido.EtapaPausada = null;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;

            if (!string.IsNullOrEmpty(dto.Usuario)) pedido.LockedBy = dto.Usuario;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Conclui a conferencia e gera os codigos de volume
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

            pedido.Status = "Aguardando Expedição";
            pedido.Volumes = dto.Volumes;
            pedido.ConfUser = dto.Usuario;
            pedido.ConfFimData = DateTime.Now;
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;
            pedido.EmEspera = false;
            pedido.EtapaPausada = null;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;

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

        // Marca o pedido como expedido
        [HttpPatch("{id}/Expedido")]
        public async Task<ActionResult<Pedido>> Expedido(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            if (pedido.Status != "Aguardando Expedição" && pedido.Status != "Aguardando Expedicao")
                throw new ExceptionTemplate($"O pedido esta {pedido.Status}");

            pedido.Status = "Expedido";
            pedido.ExpUser = dto.Usuario;
            pedido.ExpInicioData ??= DateTime.Now;
            pedido.ExpFimData = DateTime.Now;
            pedido.ErrosEvitados += dto.ErrosEvitados;
            pedido.LockedBy = null;
            pedido.EmEspera = false;
            pedido.EtapaPausada = null;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Coloca o pedido em pausa com justificativa e salva o progresso das bipagens
        [HttpPatch("{id}/pausar")]
        public async Task<ActionResult<Pedido>> PausarPedido(int id, [FromBody] PausarPedidoDTO dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .Include(p => p.VolumesPedidos)
                .Include(p => p.HistoricoPausas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            DateTime agora = DateTime.Now;
            string usuarioAtual = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario;
            string motivoFinal = string.IsNullOrWhiteSpace(dto.Motivo) ? "Não informado" : dto.Motivo.Trim();

            pedido.EmEspera = true;
            pedido.EtapaPausada = dto.Etapa;
            pedido.PausadoPor = usuarioAtual;
            pedido.DataHoraPausa = agora;
            pedido.LockedBy = null;
            pedido.ErrosEvitados += dto.ErrosEvitados;

            if ((dto.Etapa == "Expedição" || dto.Etapa == "Expedicao") && pedido.ExpInicioData == null)
            {
                pedido.ExpInicioData = agora;
                pedido.ExpUser = usuarioAtual;
            }

            if (dto.Itens != null && dto.Itens.Any())
            {
                foreach (var itemDto in dto.Itens)
                {
                    var itemDb = pedido.Itens.FirstOrDefault(i =>
                        (itemDto.Id > 0 && i.Id == itemDto.Id) ||
                        (!string.IsNullOrEmpty(itemDto.CodItem) && i.CodItem == itemDto.CodItem));

                    if (itemDb != null)
                    {
                        itemDb.ScansSep = Math.Max(itemDb.ScansSep, itemDto.ScansSep);
                        itemDb.ScansConf = Math.Max(itemDb.ScansConf, itemDto.ScansConf);
                    }
                }
            }

            if (dto.VolumesPedidos != null && dto.VolumesPedidos.Any())
            {
                foreach (var volDto in dto.VolumesPedidos)
                {
                    var volDb = pedido.VolumesPedidos.FirstOrDefault(v =>
                        (volDto.Id > 0 && v.Id == volDto.Id) ||
                        (!string.IsNullOrEmpty(volDto.CodigoVolume) && v.CodigoVolume == volDto.CodigoVolume));

                    if (volDb != null)
                    {
                        volDb.BipadoExpedicao = volDto.BipadoExpedicao;
                    }
                }
            }

            pedido.HistoricoPausas.Add(new RegistroPausa
            {
                PedidoId = pedido.Id,
                Etapa = dto.Etapa,
                OperadorPausa = usuarioAtual,
                DataHoraPausa = agora,
                Motivo = motivoFinal
            });

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Retoma um pedido que estava pausado
        [HttpPatch("{id}/retomar")]
        public async Task<ActionResult<Pedido>> RetomarPedido(int id, [FromBody] EtapaRequestDTO dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.HistoricoPausas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            DateTime agora = DateTime.Now;
            string usuarioAtual = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario;

            var pausaAberta = pedido.HistoricoPausas
                .OrderByDescending(h => h.Id)
                .FirstOrDefault(h => h.DataHoraRetomada == null);

            if (pausaAberta != null)
            {
                pausaAberta.OperadorRetomada = usuarioAtual;
                pausaAberta.DataHoraRetomada = agora;
            }

            pedido.EmEspera = false;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;
            pedido.LockedBy = usuarioAtual;

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Reverte/cancela o progresso da etapa atual gravando o motivo para o relatorio
        [HttpPatch("{id}/reverter")]
        [HttpPatch("{id}/cancelar")]
        public async Task<ActionResult<Pedido>> ReverterEtapa(int id, [FromBody] CancelarPedidoDTO? dto = null)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .Include(p => p.VolumesPedidos)
                .Include(p => p.HistoricoPausas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound(new { message = $"Pedido #{id} não encontrado." });

            DateTime agora = DateTime.Now;
            string usuarioAtual = string.IsNullOrWhiteSpace(dto?.Usuario) ? "Operador" : dto.Usuario.Trim();
            string motivoFinal = string.IsNullOrWhiteSpace(dto?.Motivo) ? "Não informado" : dto.Motivo.Trim();

            string etapaAtual = pedido.Status switch
            {
                "Em Separação" or "Em Separacao" => "Separação",
                "Em Conferencia" => "Conferência",
                "Em Expedição" or "Aguardando Expedição" or "Aguardando Expedicao" => "Expedição",
                _ => pedido.Status
            };

            var pausaAberta = pedido.HistoricoPausas
                .OrderByDescending(h => h.Id)
                .FirstOrDefault(h => h.DataHoraRetomada == null);

            if (pausaAberta != null)
            {
                pausaAberta.OperadorRetomada = $"{usuarioAtual} (Cancelado: {motivoFinal})";
                pausaAberta.DataHoraRetomada = agora;
            }
            else
            {
                pedido.HistoricoPausas.Add(new RegistroPausa
                {
                    PedidoId = pedido.Id,
                    Etapa = $"{etapaAtual} (Cancelamento)",
                    OperadorPausa = usuarioAtual,
                    DataHoraPausa = agora,
                    Motivo = motivoFinal,
                    OperadorRetomada = usuarioAtual,
                    DataHoraRetomada = agora
                });
            }

            pedido.CancelData = agora;
            pedido.CancelUser = usuarioAtual;
            pedido.MotivoCancelamento = motivoFinal;

            pedido.EmEspera = false;
            pedido.EtapaPausada = null;
            pedido.PausadoPor = null;
            pedido.DataHoraPausa = null;

            if (pedido.Status == "Em Separação" || pedido.Status == "Em Separacao")
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
            else if (pedido.Status == "Em Expedição" || pedido.Status == "Aguardando Expedição" || pedido.Status == "Aguardando Expedicao")
            {
                pedido.Status = "Aguardando Expedição";
                pedido.LockedBy = null;
                foreach (var volume in pedido.VolumesPedidos)
                {
                    volume.BipadoExpedicao = false;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Registra a solicitacao de estimativa feita pelo painel comercial
        [HttpPost("{id}/solicitar-estimativa")]
        [HttpPatch("{id}/solicitar-estimativa")]
        [HttpPut("{id}/solicitar-estimativa")]
        [HttpPost("/api/Pedido/{id}/solicitar-estimativa")]
        [HttpPatch("/api/Pedido/{id}/solicitar-estimativa")]
        public async Task<IActionResult> SolicitarEstimativa(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound(new { message = $"Pedido #{id} não foi encontrado na base de dados SQLite." });
            }

            pedido.EstimativaSolicitada = true;
            pedido.SolicitanteEstimativa = "Comercial";

            pedido.DataHoraEstimada = null;
            pedido.EstimativaSeparacao = null;
            pedido.EstimativaSeparacaoUser = null;
            pedido.EstimativaConferencia = null;
            pedido.EstimativaConferenciaUser = null;

            bool jaPassouSeparacao = pedido.Status.Contains("Confer", StringComparison.OrdinalIgnoreCase) ||
                                     pedido.Status.Contains("Exped", StringComparison.OrdinalIgnoreCase);

            if (jaPassouSeparacao)
            {
                pedido.EstimativaPendenteSeparacao = false;
                pedido.EstimativaPendenteConferencia = true;
            }
            else
            {
                pedido.EstimativaPendenteSeparacao = true;
                pedido.EstimativaPendenteConferencia = false;
            }

            await _context.SaveChangesAsync();
            return Ok(pedido);
        }

        // Grava a resposta da estimativa por etapa
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

            if (pedidoIdFinal <= 0)
            {
                return BadRequest(new { message = "O ID do pedido não foi fornecido." });
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

            string usuarioAssinatura = string.IsNullOrWhiteSpace(dtoBody?.Usuario) ? "Operador" : dtoBody.Usuario;
            string etapaInformada = dtoBody?.Etapa ?? string.Empty;

            bool respondeSeparacao = etapaInformada.Equals("Separacao", StringComparison.OrdinalIgnoreCase) ||
                                     etapaInformada.Equals("Separação", StringComparison.OrdinalIgnoreCase) ||
                                     (string.IsNullOrEmpty(etapaInformada) && pedido.EstimativaPendenteSeparacao);

            if (respondeSeparacao)
            {
                pedido.EstimativaSeparacao = dataEstimada;
                pedido.EstimativaSeparacaoUser = usuarioAssinatura;
                pedido.EstimativaPendenteSeparacao = false;
                pedido.EstimativaPendenteConferencia = true;
                pedido.DataHoraEstimada = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(pedido.EstimativaSeparacao) &&
                    DateTime.TryParseExact(pedido.EstimativaSeparacao.Trim(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtSep) &&
                    DateTime.TryParseExact(dataEstimada.Trim(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtConf))
                {
                    if (dtConf < dtSep)
                    {
                        return BadRequest(new { message = $"A previsão da conferência não pode ser anterior à da separação ({pedido.EstimativaSeparacao})." });
                    }
                }

                pedido.EstimativaConferencia = dataEstimada;
                pedido.EstimativaConferenciaUser = usuarioAssinatura;
                pedido.EstimativaPendenteSeparacao = false;
                pedido.EstimativaPendenteConferencia = false;
                pedido.EstimativaSolicitada = false;
                pedido.DataHoraEstimada = dataEstimada;
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
        public int? TipoRota { get; set; }
        public List<ItemProgressoDTO>? Itens { get; set; }
    }

    public class PausarPedidoDTO
    {
        public string Etapa { get; set; } = string.Empty;
        public string? Usuario { get; set; }
        public string? Motivo { get; set; }
        public int ErrosEvitados { get; set; }
        public List<ItemProgressoDTO>? Itens { get; set; }
        public List<VolumeProgressoDTO>? VolumesPedidos { get; set; }
    }

    public class CancelarPedidoDTO
    {
        public string? Usuario { get; set; }
        public string? Motivo { get; set; }
    }

    public class ItemProgressoDTO
    {
        public int Id { get; set; }
        public string CodItem { get; set; } = string.Empty;
        public int ScansSep { get; set; }
        public int ScansConf { get; set; }
    }

    public class VolumeProgressoDTO
    {
        public int Id { get; set; }
        public string CodigoVolume { get; set; } = string.Empty;
        public bool BipadoExpedicao { get; set; }
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
        public string? Etapa { get; set; }
        public string? Usuario { get; set; }
    }
}