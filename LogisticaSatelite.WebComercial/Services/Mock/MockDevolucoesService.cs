using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Mock;

public class MockDevolucoesService : IDevolucoesService
{
    private readonly MockDataStore _store;

    public MockDevolucoesService(MockDataStore store)
    {
        _store = store;
    }

    public Task<(bool Sucesso, PedidoClienteDevolucaoDto? Dados, string? Erro)> BuscarPedidoClienteAsync(int pedidoId)
    {
        var pedido = _store.Pedidos.FirstOrDefault(p => p.Id == pedidoId);
        if (pedido == null)
            return Task.FromResult<(bool, PedidoClienteDevolucaoDto?, string?)>((false, null, $"Pedido #{pedidoId} não encontrado no modo Mock."));

        if (!pedido.Status.Equals("Expedido", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<(bool, PedidoClienteDevolucaoDto?, string?)>((false, null, $"O pedido #{pedidoId} está '{pedido.Status}'. Apenas pedidos expedidos podem ser devolvidos."));

        var dto = new PedidoClienteDevolucaoDto
        {
            PedidoId = pedido.Id,
            ClienteNome = pedido.Destinatario,
            Itens = new List<ItemDisponivelDevolucaoDto>
            {
                new ItemDisponivelDevolucaoDto { CodItem = "001", Nome = "Caixa Organizadora Plástica 30L", QuantidadeComprada = 4, QuantidadeDisponivel = 4 },
                new ItemDisponivelDevolucaoDto { CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", QuantidadeComprada = 3, QuantidadeDisponivel = 3 }
            }
        };

        return Task.FromResult<(bool, PedidoClienteDevolucaoDto?, string?)>((true, dto, null));
    }

    public Task<(bool Sucesso, DevolucaoDto? Devolucao, string? Erro)> SolicitarDevolucaoAsync(CriarDevolucaoRequestDto request)
    {
        if (request.EmiteNotaFiscal && string.IsNullOrWhiteSpace(request.NumeroNotaDevolucao))
            return Task.FromResult<(bool, DevolucaoDto?, string?)>((false, null, "Informe o número/chave da Nota Fiscal de Devolução."));

        int novoId = _store.Devolucoes.Any() ? _store.Devolucoes.Max(d => d.Id) + 1 : 1;

        var nova = new DevolucaoDto
        {
            Id = novoId,
            PedidoId = request.PedidoId,
            ClienteNome = request.ClienteNome.Trim(),
            DocumentoCliente = request.DocumentoCliente.Trim(),
            EmiteNotaFiscal = request.EmiteNotaFiscal,
            NumeroNotaDevolucao = request.EmiteNotaFiscal ? request.NumeroNotaDevolucao?.Trim() : null,
            MotivoGeral = string.IsNullOrWhiteSpace(request.MotivoGeral) ? "Solicitação do cliente" : request.MotivoGeral.Trim(),
            VolumesInformados = Math.Max(1, request.VolumesInformados),
            Status = "Pendente Analise",
            DataSolicitacao = DateTime.Now,
            Itens = request.Itens.Where(i => i.Quantidade > 0).Select((i, idx) => new ItemDevolucaoDto
            {
                Id = idx + 1,
                DevolucaoId = novoId,
                CodItem = i.CodItem,
                Nome = i.CodItem == "001" ? "Caixa Organizadora Plástica 30L" : "Fita Adesiva Transparente 45mm x 100m",
                QuantidadeSolicitada = i.Quantidade
            }).ToList()
        };

        _store.Devolucoes.Insert(0, nova);
        return Task.FromResult<(bool, DevolucaoDto?, string?)>((true, nova, null));
    }

    public Task<(bool Sucesso, List<DevolucaoDto> Lista, string? Erro)> ListarDevolucoesAsync()
    {
        var lista = _store.Devolucoes.OrderByDescending(d => d.Id).ToList();
        return Task.FromResult<(bool, List<DevolucaoDto>, string?)>((true, lista, null));
    }

    public Task<(bool Sucesso, DevolucaoDto? Devolucao, string? Erro)> AnalisarDevolucaoAsync(int id, bool aprovada, string usuario, string? motivoRejeicao = null)
    {
        var dev = _store.Devolucoes.FirstOrDefault(d => d.Id == id);
        if (dev == null)
            return Task.FromResult<(bool, DevolucaoDto?, string?)>((false, null, "Devolução não encontrada."));

        dev.AnalisadoPor = usuario;
        dev.DataAnalise = DateTime.Now;

        if (aprovada)
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
            if (string.IsNullOrWhiteSpace(motivoRejeicao))
                return Task.FromResult<(bool, DevolucaoDto?, string?)>((false, null, "Informe o motivo da rejeição."));

            dev.Status = "Rejeitada";
            dev.MotivoRejeicao = motivoRejeicao.Trim();
        }

        return Task.FromResult<(bool, DevolucaoDto?, string?)>((true, dev, null));
    }

    public Task<(bool Sucesso, List<ProdutoAvariadoDto> Lista, string? Erro)> ListarAvariadosAsync()
    {
        var lista = _store.ProdutosAvariados.OrderByDescending(a => a.Id).ToList();
        return Task.FromResult<(bool, List<ProdutoAvariadoDto>, string?)>((true, lista, null));
    }

    public Task<(bool Sucesso, string? Erro)> BaixarAvariadoAsync(int id)
    {
        var item = _store.ProdutosAvariados.FirstOrDefault(a => a.Id == id);
        if (item == null)
            return Task.FromResult<(bool, string?)>((false, "Registro não encontrado."));

        _store.ProdutosAvariados.Remove(item);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}
