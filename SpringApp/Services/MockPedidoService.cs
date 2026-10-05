using SpringApp.Models;

namespace SpringApp.Services;

public class MockPedidoService : IPedidoService
{
    private static List<Pedido> _pedidos = new()
    {
        new Pedido
        {
            IdPedido = 9999,
            Cliente = "Vinicius Ferreira Amorim",
            Modal = "Transportadora",
            Status = "Aguardando Expediçao",
            Volumes = 1,
            EstimativaSolicitada = true,
            SolicitanteEstimativa = "Comercial (Web)",
            DataHoraEstimada = ""
        },
        new Pedido
        {
            IdPedido = 1,
            Cliente = "Vinicius Amorim",
            Modal = "Transportadora",
            Status = "Aguardando",
            Volumes = 2,
            EstimativaSolicitada = false
        },
        new Pedido
        {
            IdPedido = 2,
            Cliente = "Vinicius Ferreira",
            Modal = "Retira",
            Status = "Aguardando",
            Volumes = 1,
            EstimativaSolicitada = false
        },
        new Pedido
        {
            IdPedido = 3,
            Cliente = "Vinicius Amorim",
            Modal = "Transportadora",
            Status = "Aguardando Conferencia",
            Volumes = 3,
            EstimativaSolicitada = false
        }
    };

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        await Task.Delay(300);
        return _pedidos.ToList();
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        await Task.Delay(300);
        return _pedidos.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        await Task.Delay(300);
        return _pedidos.FirstOrDefault(p => p.IdPedido == id);
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        await Task.Delay(300);
        return (true, "Pedido bloqueado com sucesso (Mock).");
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(300);
        var pedido = _pedidos.FirstOrDefault(p => p.IdPedido == id);

        if (pedido == null)
            return (false, "Pedido não encontrado.");

        if (etapaAtual.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
            pedido.Status = "Em Separaçao";
        else if (etapaAtual.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase))
            pedido.Status = "Em Conferencia";
        else if (etapaAtual.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase))
            pedido.Status = "Em Expediçao";

        return (true, "Status atualizado com sucesso!");
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(300);
        return (true, "Conferência concluída (Mock).");
    }

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id)
    {
        await Task.Delay(300);
        return (true, "Etapa revertida (Mock).");
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        await Task.Delay(300);
        return "url-relatorio-mock.pdf";
    }

    public async Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada)
    {
        await Task.Delay(300);
        var pedido = _pedidos.FirstOrDefault(p => p.IdPedido == id);
        if (pedido != null)
        {
            pedido.DataHoraEstimada = dataHoraEstimada;
            pedido.EstimativaSolicitada = false;
            return (true, string.Empty);
        }
        return (false, "Pedido não encontrado no Mock.");
    }
}