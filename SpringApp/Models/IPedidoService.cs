using SpringApp.Models;

namespace SpringApp.Services;

public interface IPedidoService
{
    Task<List<Pedido>> GetPedidosAsync();
    Task<List<Pedido>> ObterPorStatusAsync(string status);
    Task<List<Pedido>> ObterPorClienteAsync(string termoCliente);
    Task<Pedido?> GetPedidoPorIdAsync(int id);
    Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario);
    Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0);
    Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0);
    Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id, string? motivo = null, string? usuario = null);
    Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario, string? motivo = null);
    Task<(bool Sucesso, string Mensagem)> PausarPedidoAsync(Pedido pedido, string etapa, string usuario, int errosEvitados = 0, string? motivo = null);
    Task<(bool Sucesso, string Mensagem)> RetomarPedidoAsync(int id, string usuario);
    Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro);
    Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada, string etapa = "", string usuario = "");
    Task<List<Cotacao>> GetCotacoesPendentesAsync();
    Task<(bool Sucesso, string Mensagem)> ResponderCotacaoAsync(int id, int volumes, double pesoTotalKg, string dimensoes, string usuario);
}