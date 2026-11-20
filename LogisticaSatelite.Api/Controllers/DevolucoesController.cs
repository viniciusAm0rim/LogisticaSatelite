using LogisticaSatelite.Api.Data;
using LogisticaSatelite.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticaSatelite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevolucoesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DevolucoesController(AppDbContext context)
        {
            _context = context;
        }

        // Busca dados basicos de um pedido expedido para preencher o formulario publico do cliente
        [HttpGet("pedido-cliente/{pedidoId}")]
        public async Task<IActionResult> BuscarPedidoParaCliente(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                return NotFound(new { message = $"Pedido #{pedidoId} não encontrado." });

            if (!pedido.Status.Equals("Expedido", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = $"O pedido #{pedidoId} está com status '{pedido.Status}'. Apenas pedidos já expedidos podem ser devolvidos." });

            var devolucoesAtivas = await _context.Devolucoes
                .Include(d => d.Itens)
                .Where(d => d.PedidoId == pedidoId && d.Status != "Rejeitada" && d.Status != "Recusada na Doca")
                .ToListAsync();

            var itensDisponiveis = pedido.Itens.Select(i =>
            {
                int jaSolicitado = devolucoesAtivas
                    .SelectMany(d => d.Itens)
                    .Where(di => di.CodItem == i.CodItem)
                    .Sum(di => di.QuantidadeSolicitada);

                return new
                {
                    codItem = i.CodItem,
                    nome = i.Nome,
                    quantidadeComprada = i.Quantidade,
                    quantidadeDisponivel = Math.Max(0, i.Quantidade - jaSolicitado)
                };
            }).Where(x => x.quantidadeDisponivel > 0).ToList();

            if (!itensDisponiveis.Any())
                return BadRequest(new { message = "Todos os itens deste pedido já possuem solicitação de devolução ativa ou concluída." });

            return Ok(new
            {
                pedidoId = pedido.Id,
                clienteNome = pedido.Destinatario,
                itens = itensDisponiveis
            });
        }

        // Cliente envia o formulario publico de solicitacao de devolucao
        [HttpPost]
        public async Task<IActionResult> SolicitarDevolucao([FromBody] CriarDevolucaoDTO dto)
        {
            if (dto == null || dto.PedidoId <= 0)
                return BadRequest(new { message = "Número do pedido inválido." });

            if (string.IsNullOrWhiteSpace(dto.ClienteNome) || string.IsNullOrWhiteSpace(dto.DocumentoCliente))
                return BadRequest(new { message = "Informe o nome e o CPF/CNPJ do cliente." });

            if (dto.EmiteNotaFiscal && string.IsNullOrWhiteSpace(dto.NumeroNotaDevolucao))
                return BadRequest(new { message = "Clientes que emitem NF devem informar o número/chave da Nota Fiscal de Devolução." });

            if (dto.Itens == null || !dto.Itens.Any(i => i.Quantidade > 0))
                return BadRequest(new { message = "Selecione ao menos um item com quantidade maior que zero." });

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == dto.PedidoId);

            if (pedido == null)
                return NotFound(new { message = $"Pedido #{dto.PedidoId} não encontrado." });

            var devolucao = new Devolucao
            {
                PedidoId = pedido.Id,
                ClienteNome = dto.ClienteNome.Trim(),
                DocumentoCliente = dto.DocumentoCliente.Trim(),
                EmiteNotaFiscal = dto.EmiteNotaFiscal,
                NumeroNotaDevolucao = dto.EmiteNotaFiscal ? dto.NumeroNotaDevolucao?.Trim() : null,
                MotivoGeral = string.IsNullOrWhiteSpace(dto.MotivoGeral) ? "Solicitação do cliente" : dto.MotivoGeral.Trim(),
                VolumesInformados = Math.Max(1, dto.VolumesInformados),
                Status = "Pendente Analise",
                DataSolicitacao = DateTime.Now
            };

            foreach (var itemInput in dto.Itens.Where(i => i.Quantidade > 0))
            {
                var itemPedido = pedido.Itens.FirstOrDefault(p => p.CodItem == itemInput.CodItem);
                if (itemPedido == null)
                    return BadRequest(new { message = $"O item '{itemInput.CodItem}' não pertence ao pedido #{pedido.Id}." });

                if (itemInput.Quantidade > itemPedido.Quantidade)
                    return BadRequest(new { message = $"Quantidade solicitada de '{itemPedido.Nome}' excede a quantidade do pedido." });

                devolucao.Itens.Add(new ItemDevolucao
                {
                    CodItem = itemPedido.CodItem,
                    Nome = itemPedido.Nome,
                    QuantidadeSolicitada = itemInput.Quantidade
                });
            }

            _context.Devolucoes.Add(devolucao);
            await _context.SaveChangesAsync();

            return Ok(devolucao);
        }

        // Lista todas as devolucoes (opcionalmente filtrando por status)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Devolucao>>> GetDevolucoes([FromQuery] string? status)
        {
            var query = _context.Devolucoes
                .Include(d => d.Itens)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.Status.ToLower() == status.ToLower());
            }

            var lista = await query
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return Ok(lista);
        }

        // ADM analisa a solicitacao: Autoriza (gerando codigo para PF) ou Rejeita
        [HttpPatch("{id}/analisar")]
        public async Task<IActionResult> AnalisarDevolucao(int id, [FromBody] AnalisarDevolucaoDTO dto)
        {
            var dev = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dev == null)
                return NotFound(new { message = $"Devolução #{id} não encontrada." });

            string usuarioAdm = string.IsNullOrWhiteSpace(dto.Usuario) ? "ADM" : dto.Usuario.Trim();
            dev.AnalisadoPor = usuarioAdm;
            dev.DataAnalise = DateTime.Now;

            if (dto.Aprovada)
            {
                dev.Status = "Autorizada";
                dev.MotivoRejeicao = null;

                if (!dev.EmiteNotaFiscal && string.IsNullOrWhiteSpace(dev.CodigoAutorizacao))
                {
                    dev.CodigoAutorizacao = $"DEV-{dev.Id:D4}";
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.MotivoRejeicao))
                    return BadRequest(new { message = "Informe o motivo da rejeição." });

                dev.Status = "Rejeitada";
                dev.MotivoRejeicao = dto.MotivoRejeicao.Trim();
            }

            await _context.SaveChangesAsync();
            return Ok(dev);
        }

        // Etapa 1 no App: Operador bipa o Codigo de Autorizacao (PF) ou NF de Devolucao (PJ) e confirma os volumes.
        // Se o codigo nao bater, apenas alerta sem recusar automaticamente.
        [HttpPatch("{id}/conferir-volumes")]
        public async Task<IActionResult> ConferirVolumes(int id, [FromBody] ConferirVolumesDevolucaoDTO dto)
        {
            var dev = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dev == null)
                return NotFound(new { message = $"Devolução #{id} não encontrada." });

            if (!dev.Status.Equals("Autorizada", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Esta devolução está '{dev.Status}' e não está liberada para bipagem de volumes." });

            string codigoEsperado = dev.EmiteNotaFiscal
                ? (dev.NumeroNotaDevolucao ?? string.Empty)
                : (dev.CodigoAutorizacao ?? string.Empty);

            if (string.IsNullOrWhiteSpace(dto.CodigoLido) ||
                !dto.CodigoLido.Trim().Equals(codigoEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                string tipoDoc = dev.EmiteNotaFiscal ? "Nota Fiscal de Devolução" : "Código de Autorização";
                return BadRequest(new { message = $"{tipoDoc} não confere com o cadastrado ('{codigoEsperado}'). Verifique e tente novamente ou clique em Recusar." });
            }

            if (dto.VolumesRecebidos <= 0)
                return BadRequest(new { message = "Informe a quantidade de volumes recebidos." });

            dev.VolumesRecebidos = dto.VolumesRecebidos;
            dev.ConferidoPor = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario.Trim();
            dev.Status = "Volumes Conferidos";

            await _context.SaveChangesAsync();
            return Ok(dev);
        }

        // Etapa 2 no App: Valida a contagem dos itens bipados. Se divergir, avisa o operador sem recusar sozinho.
        [HttpPatch("{id}/validar-itens")]
        public async Task<IActionResult> ValidarItensBipados(int id, [FromBody] ValidarItensDevolucaoDTO dto)
        {
            var dev = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dev == null)
                return NotFound(new { message = $"Devolução #{id} não encontrada." });

            string usuarioOp = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario.Trim();
            var divergencias = new List<string>();

            foreach (var item in dev.Itens)
            {
                var bipadoDto = dto.ItensBipados?.FirstOrDefault(x => x.CodItem.Equals(item.CodItem, StringComparison.OrdinalIgnoreCase));
                int qtdLida = bipadoDto?.QuantidadeBipada ?? 0;
                item.QuantidadeBipada = qtdLida;

                if (qtdLida != item.QuantidadeSolicitada)
                {
                    divergencias.Add($"{item.Nome} (Esperado: {item.QuantidadeSolicitada}, Bipado: {qtdLida})");
                }
            }

            if (dto.ItensBipados != null)
            {
                foreach (var extra in dto.ItensBipados.Where(x => x.QuantidadeBipada > 0))
                {
                    if (!dev.Itens.Any(i => i.CodItem.Equals(extra.CodItem, StringComparison.OrdinalIgnoreCase)))
                    {
                        divergencias.Add($"Item não autorizado: {extra.CodItem} (Bipado: {extra.QuantidadeBipada})");
                    }
                }
            }

            dev.ConferidoPor = usuarioOp;
            dev.DataConferencia = DateTime.Now;

            if (divergencias.Any())
            {
                // Mantem o status atual para o operador corrigir a bipagem ou clicar no botao Recusar
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    aprovadaNaConferencia = false,
                    mensagem = "Contagem divergente do formulário:\n• " + string.Join("\n• ", divergencias) + "\n\nCorrija a bipagem ou clique em 'Recusar' para rejeitar na doca.",
                    divergencias,
                    devolucao = dev
                });
            }

            dev.Status = "Em Classificação";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                aprovadaNaConferencia = true,
                mensagem = "Itens conferidos com sucesso! Selecione o estado de cada item (Bom Estado ou Avariado).",
                divergencias = Array.Empty<string>(),
                devolucao = dev
            });
        }

        // Recusa explicita acionada pelo operador no App (na etapa de Volumes ou Itens)
        [HttpPatch("{id}/recusar")]
        public async Task<IActionResult> RecusarNaDoca(int id, [FromBody] RecusarDevolucaoDocaDTO dto)
        {
            var dev = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dev == null)
                return NotFound(new { message = $"Devolução #{id} não encontrada." });

            if (string.IsNullOrWhiteSpace(dto.MotivoRejeicao))
                return BadRequest(new { message = "Informe o motivo da recusa na doca." });

            dev.Status = "Recusada na Doca";
            dev.MotivoRejeicao = dto.MotivoRejeicao.Trim();
            dev.ConferidoPor = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario.Trim();
            dev.DataConferencia = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(dev);
        }

        // Etapa 3 no App: Classifica itens em Bom Estado (soma ao estoque) ou Avariado (alimenta relatorio de Avariados)
        [HttpPatch("{id}/classificar-itens")]
        public async Task<IActionResult> ClassificarItens(int id, [FromBody] ClassificarItensDevolucaoDTO dto)
        {
            var dev = await _context.Devolucoes
                .Include(d => d.Itens)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dev == null)
                return NotFound(new { message = $"Devolução #{id} não encontrada." });

            if (!dev.Status.Equals("Em Classificação", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Status inválido para classificação: '{dev.Status}'." });

            string usuarioOp = string.IsNullOrWhiteSpace(dto.Usuario) ? "Operador" : dto.Usuario.Trim();
            DateTime agora = DateTime.Now;

            foreach (var item in dev.Itens)
            {
                var classif = dto.ItensClassificados?.FirstOrDefault(c => c.CodItem.Equals(item.CodItem, StringComparison.OrdinalIgnoreCase));
                if (classif == null)
                    return BadRequest(new { message = $"Classificação não informada para o item '{item.Nome}'." });

                if (classif.QuantidadeBomEstado < 0 || classif.QuantidadeAvariada < 0 ||
                    (classif.QuantidadeBomEstado + classif.QuantidadeAvariada) != item.QuantidadeSolicitada)
                {
                    return BadRequest(new { message = $"A soma de Bom Estado + Avariado de '{item.Nome}' deve ser igual a {item.QuantidadeSolicitada}." });
                }

                item.QuantidadeBomEstado = classif.QuantidadeBomEstado;
                item.QuantidadeAvariada = classif.QuantidadeAvariada;
                item.MotivoAvaria = classif.QuantidadeAvariada > 0
                    ? (string.IsNullOrWhiteSpace(classif.MotivoAvaria) ? "Avaria constatada na triagem" : classif.MotivoAvaria.Trim())
                    : null;

                if (classif.QuantidadeBomEstado > 0)
                {
                    var produtoDb = await _context.Produtos.FirstOrDefaultAsync(p => p.CodItem == item.CodItem);
                    if (produtoDb != null)
                    {
                        produtoDb.QtdPicking += classif.QuantidadeBomEstado;
                    }
                }

                if (classif.QuantidadeAvariada > 0)
                {
                    _context.ProdutosAvariados.Add(new ProdutoAvariado
                    {
                        CodItem = item.CodItem,
                        Nome = item.Nome,
                        Quantidade = classif.QuantidadeAvariada,
                        Origem = $"Devolução #{dev.Id:D4}",
                        DevolucaoId = dev.Id,
                        PedidoId = dev.PedidoId,
                        MotivoAvaria = item.MotivoAvaria!,
                        RegistradoPor = usuarioOp,
                        DataRegistro = agora
                    });
                }
            }

            dev.Status = "Concluida";
            dev.ConferidoPor = usuarioOp;
            dev.DataConferencia = agora;

            await _context.SaveChangesAsync();
            return Ok(dev);
        }
    }

    public class CriarDevolucaoDTO
    {
        public int PedidoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string DocumentoCliente { get; set; } = string.Empty;
        public bool EmiteNotaFiscal { get; set; }
        public string? NumeroNotaDevolucao { get; set; }
        public string MotivoGeral { get; set; } = string.Empty;
        public int VolumesInformados { get; set; } = 1;
        public List<ItemDevolucaoInputDTO> Itens { get; set; } = new();
    }

    public class ItemDevolucaoInputDTO
    {
        public string CodItem { get; set; } = string.Empty;
        public int Quantidade { get; set; }
    }

    public class AnalisarDevolucaoDTO
    {
        public bool Aprovada { get; set; }
        public string? Usuario { get; set; }
        public string? MotivoRejeicao { get; set; }
    }

    public class ConferirVolumesDevolucaoDTO
    {
        public string CodigoLido { get; set; } = string.Empty;
        public int VolumesRecebidos { get; set; }
        public string? Usuario { get; set; }
    }

    public class ValidarItensDevolucaoDTO
    {
        public string? Usuario { get; set; }
        public List<ItemBipadoDevolucaoDTO> ItensBipados { get; set; } = new();
    }

    public class ItemBipadoDevolucaoDTO
    {
        public string CodItem { get; set; } = string.Empty;
        public int QuantidadeBipada { get; set; }
    }

    public class RecusarDevolucaoDocaDTO
    {
        public string? Usuario { get; set; }
        public string MotivoRejeicao { get; set; } = string.Empty;
    }

    public class ClassificarItensDevolucaoDTO
    {
        public string? Usuario { get; set; }
        public List<ItemClassificadoDevolucaoDTO> ItensClassificados { get; set; } = new();
    }

    public class ItemClassificadoDevolucaoDTO
    {
        public string CodItem { get; set; } = string.Empty;
        public int QuantidadeBomEstado { get; set; }
        public int QuantidadeAvariada { get; set; }
        public string? MotivoAvaria { get; set; }
    }
}