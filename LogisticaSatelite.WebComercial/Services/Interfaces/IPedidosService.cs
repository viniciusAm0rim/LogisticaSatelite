using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services.Interfaces;

public record ResultadoConsultaPedidos(bool Sucesso, List<PedidoDto> Pedidos, string? MensagemErro = null);

public interface IPedidosService
{
    Task<ResultadoConsultaPedidos> ObterPedidosAsync();
    Task<bool> SolicitarEstimativaAsync(int pedidoId);
}