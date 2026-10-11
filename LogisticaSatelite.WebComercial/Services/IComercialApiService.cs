using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services;

public interface IComercialApiService
{
    Task<List<PedidoDto>> ObterPedidosAsync();
    Task<PedidoDto?> ObterPedidoPorIdAsync(int id);
    Task<bool> SolicitarEstimativaAsync(int pedidoId);
}