using LogisticaSatelite.WebComercial.Models;
using LogisticaSatelite.WebComercial.Services.Interfaces;

namespace LogisticaSatelite.WebComercial.Services.Mock;

public class MockPedidosService : IPedidosService
{
    private readonly MockDataStore _store;

    public MockPedidosService(MockDataStore store)
    {
        _store = store;
    }

    public Task<ResultadoConsultaPedidos> ObterPedidosAsync()
    {
        var copia = _store.Pedidos.ToList();
        return Task.FromResult(new ResultadoConsultaPedidos(true, copia));
    }

    public Task<bool> SolicitarEstimativaAsync(int pedidoId)
    {
        var pedido = _store.Pedidos.FirstOrDefault(p => p.Id == pedidoId);
        if (pedido == null)
            return Task.FromResult(false);

        pedido.EstimativaSolicitada = true;
        pedido.SolicitanteEstimativa = "Comercialmock";
        return Task.FromResult(true);
    }
}