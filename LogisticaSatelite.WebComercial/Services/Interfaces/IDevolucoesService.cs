using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services.Interfaces;

public interface IDevolucoesService
{
    Task<(bool Sucesso, PedidoClienteDevolucaoDto? Dados, string? Erro)> BuscarPedidoClienteAsync(int pedidoId);
    Task<(bool Sucesso, DevolucaoDto? Devolucao, string? Erro)> SolicitarDevolucaoAsync(CriarDevolucaoRequestDto request);
    Task<(bool Sucesso, List<DevolucaoDto> Lista, string? Erro)> ListarDevolucoesAsync();
    Task<(bool Sucesso, DevolucaoDto? Devolucao, string? Erro)> AnalisarDevolucaoAsync(int id, bool aprovada, string usuario, string? motivoRejeicao = null);
    Task<(bool Sucesso, List<ProdutoAvariadoDto> Lista, string? Erro)> ListarAvariadosAsync();
    Task<(bool Sucesso, string? Erro)> BaixarAvariadoAsync(int id);
}