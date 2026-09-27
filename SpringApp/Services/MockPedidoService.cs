using SpringApp.Models;

namespace SpringApp.Services;

public class MockPedidoService : IPedidoService
{
    private static List<Pedido> _pedidosMock = new List<Pedido>
    {
        // Pedido 1 (Loja)
        new Pedido
        {
            IdPedido = 1,
            Cliente = "Vinicius Ferreira Amorim",
            Modal = "Loja",
            NomeTransportadora = "Logística Rápida Expresso",
            CodTransportadora = "CTR-99",
            Status = "Aguardando",
            Volumes = 0,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 1, CodItem = "SKU-001", LocalPicking = "R1-A-01", Quantidade = 2 },
                new ItemPedido { ProdutoId = 2, CodItem = "SKU-002", LocalPicking = "R2-A-02", Quantidade = 1 }
            }
        },

        // Pedido 2 (Retira)
        new Pedido
        {
            IdPedido = 2,
            Cliente = "Thaís Martins",
            Modal = "Retira",
            NomeTransportadora = "Retirada Local",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 3, CodItem = "SKU-003", LocalPicking = "R3-C-10", Quantidade = 5 }
            }
        }
    };

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        await Task.Delay(300);
        return _pedidosMock;
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        await Task.Delay(300);
        return _pedidosMock.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Pedido> GetPedidoPorIdAsync(int id)
    {
        await Task.Delay(200);
        return _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        await Task.Delay(200);
        return (true, string.Empty);
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(500);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        DateTime dataHoraAtual = DateTime.Now;

        if (mock.Status == "Aguardando")
        {
            mock.Status = "Em Separaçao";
            mock.SepInicioData = dataHoraAtual;
        }
        else if (mock.Status == "Em Separaçao")
        {
            mock.Status = "Aguardando Conferencia";
            mock.SepUser = usuario;
            mock.SepFimData = dataHoraAtual;
            mock.ErrosEvitados += errosEvitados;
        }
        else if (mock.Status == "Aguardando Conferencia")
        {
            mock.Status = "Em Conferencia";
            mock.ConfInicioData = dataHoraAtual;
        }
        else if (mock.Status == "Aguardando Expediçao")
        {
            mock.Status = "Expedido";
            mock.ExpUser = usuario;
            mock.ExpFimData = dataHoraAtual;
            mock.ErrosEvitados += errosEvitados;
        }

        return (true, "Sucesso");
    }

    // GERAÇÃO AUTOMÁTICA DOS CÓDIGOS DE VOLUME NO MOCK (Ex: c1p1v1)
    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(500);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        mock.Status = "Aguardando Expediçao";
        mock.Volumes = volumes;
        mock.ConfUser = usuario;
        mock.ConfFimData = DateTime.Now;
        mock.ErrosEvitados += errosEvitados;

        // Limpa volumes anteriores e gera os novos automaticamente no formato c{ClienteId}p{PedidoId}v{i}
        mock.VolumesPedidos.Clear();
        int clienteId = 1; // ID simulado para o cliente no mock

        for (int i = 1; i <= volumes; i++)
        {
            mock.VolumesPedidos.Add(new VolumePedidoDto
            {
                Id = i,
                CodigoVolume = $"c{clienteId}p{mock.IdPedido}v{i}",
                BipadoExpedicao = false
            });
        }

        return (true, "Sucesso");
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        await Task.Delay(500);
        string fileName = $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        var linhasCsv = new List<string>
        {
            "ID Pedido;Cliente;Status;Sep User;Conf User;Exp User;Hora Inicio Separacao;Hora Fim Separacao;Hora Inicio Conferencia;Hora Fim Conferencia;Data Expedicao;Erros Evitados;Tempo Medio (min)"
        };

        foreach (var p in _pedidosMock)
        {
            if (!string.IsNullOrEmpty(operadorFiltro) && operadorFiltro != "Todos")
            {
                if (p.SepUser != operadorFiltro && p.ConfUser != operadorFiltro && p.ExpUser != operadorFiltro)
                    continue;
            }

            string sepInicio = p.SepInicioData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string sepFim = p.SepFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string confInicio = p.ConfInicioData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string confFim = p.ConfFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string expFim = p.ExpFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

            linhasCsv.Add($"{p.IdPedido};{p.Cliente};{p.Status};{p.SepUser ?? "-"};{p.ConfUser ?? "-"};{p.ExpUser ?? "-"};{sepInicio};{sepFim};{confInicio};{confFim};{expFim};{p.ErrosEvitados};10,00");
        }

        await File.WriteAllLinesAsync(filePath, linhasCsv, System.Text.Encoding.UTF8);
        return filePath;
    }

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id)
    {
        await Task.Delay(300);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        if (mock.Status == "Em Separaçao")
        {
            mock.Status = "Aguardando";
            mock.SepInicioData = null;
        }
        else if (mock.Status == "Em Conferencia")
        {
            mock.Status = "Aguardando Conferencia";
            mock.ConfInicioData = null;
        }

        return (true, "Sucesso");
    }
}

