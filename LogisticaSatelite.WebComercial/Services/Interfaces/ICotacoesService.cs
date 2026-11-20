using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services.Interfaces;

public interface ICotacoesService
{
    Task<List<CotacaoDto>> ObterCotacoesAsync();
    Task<List<ProdutoCatalogoDto>> BuscarProdutosAsync(string termo);
    Task<(bool Sucesso, string? MensagemErro)> CriarCotacaoAsync(NovaCotacaoRequestDto novaCotacao);
}