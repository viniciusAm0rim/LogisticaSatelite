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
        pedido.SolicitanteEstimativa = "Comercial";
        pedido.DataHoraEstimada = null;
        pedido.EstimativaConferencia = null;
        pedido.EstimativaConferenciaUser = null;

        bool jaPassouSeparacao = pedido.Status.Contains("Confer", StringComparison.OrdinalIgnoreCase) ||
                                 pedido.Status.Contains("Exped", StringComparison.OrdinalIgnoreCase);

        pedido.EstimativaPendenteSeparacao = !jaPassouSeparacao;
        pedido.EstimativaPendenteConferencia = jaPassouSeparacao;

        return Task.FromResult(true);
    }
}