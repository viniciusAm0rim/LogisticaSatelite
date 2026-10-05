using SpringApp.Models;

namespace SpringApp.Services;

public interface IPedidoService
{
    Task<List<Pedido>> GetPedidosAsync();
    Task<List<Pedido>> ObterPorStatusAsync(string status);
    Task<Pedido?> GetPedidoPorIdAsync(int id);
    Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario);
    Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0);
    Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0);
    Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id);
    Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro);
    Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada);
}