using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Mock;

public class MockCotacoesService : ICotacoesService
{
    private readonly List<CotacaoDto> _cotacoesMock = new();
    private int _proximoId = 1;

    public Task<List<CotacaoDto>> ObterCotacoesAsync()
    {
        return Task.FromResult(_cotacoesMock.OrderByDescending(c => c.Id).ToList());
    }

    public Task<List<ProdutoCatalogoDto>> BuscarProdutosAsync(string termo)
    {
        var catalogo = new List<ProdutoCatalogoDto>
        {
            new() { Id = 1, CodItem = "SKU-001", Nome = "Caixa Papelão Reforçada 40x30" },
            new() { Id = 2, CodItem = "SKU-002", Nome = "Fita Adesiva Industrial 48mm" },
            new() { Id = 3, CodItem = "SKU-003", Nome = "Filme Stretch 500mm" }
        };

        if (string.IsNullOrWhiteSpace(termo))
            return Task.FromResult(catalogo);

        var filtrados = catalogo
            .Where(p => p.CodItem.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                        p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(filtrados);
    }

    public Task<(bool Sucesso, string? MensagemErro)> CriarCotacaoAsync(NovaCotacaoRequestDto novaCotacao)
    {
        var item = new CotacaoDto
        {
            Id = _proximoId++,
            Cliente = novaCotacao.Cliente,
            Observacao = novaCotacao.Observacao,
            Solicitante = novaCotacao.Solicitante,
            DataSolicitacao = DateTime.Now,
            Status = "Pendente",
            Itens = novaCotacao.Itens.Select(i => new ItemCotacaoDto
            {
                CodItem = i.CodItem,
                Nome = i.Nome,
                Quantidade = i.Quantidade
            }).ToList()
        };

        _cotacoesMock.Add(item);
        return Task.FromResult((true, (string?)null));
    }
}