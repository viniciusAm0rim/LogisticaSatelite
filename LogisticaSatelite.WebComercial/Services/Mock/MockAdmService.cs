using LogisticaSatelite.WebComercial.Services.Interfaces;
using LogisticaSatelite.WebComercial.Services.Producao;

namespace LogisticaSatelite.WebComercial.Services.Mock;

public class MockAdmService : IAdmService
{
    private readonly MockDataStore _store;

    public MockAdmService(MockDataStore store)
    {
        _store = store;
    }

    public Task<ResultadoDashboardAdm> ObterDashboardAsync(string funcionario = "Todos")
    {
        var dashboard = ProducaoAdmService.CalcularIndicadores(_store.Pedidos, funcionario);
        return Task.FromResult(new ResultadoDashboardAdm(true, dashboard));
    }
}