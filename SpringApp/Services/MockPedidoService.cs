using SpringApp.Models;

namespace SpringApp.Services;

public class MockPedidoService : IPedidoService
{
    private static List<Pedido> _pedidosMock = new List<Pedido>
    {
        // Pedido 1 - Aguardando + Estimativa Solicitada pelo Comercial
        new Pedido
        {
            IdPedido = 101,
            Cliente = "AcquAmorim",
            Modal = "Loja",
            NomeTransportadora = "Cliente Loja",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            EstimativaSolicitada = true,
            SolicitanteEstimativa = "Comercial (Web)",
            DataHoraEstimada = "",
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 1, CodItem = "001", Nome = "Caixa Organizadora Plástica 30L", LocalPicking = "R1-A-01", Quantidade = 1 },
                new ItemPedido { ProdutoId = 2, CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", LocalPicking = "R2-A-05", Quantidade = 1 }
            }
        },

        // Pedido 2 - Em Separação
        new Pedido
        {
            IdPedido = 102,
            Cliente = "Vinicius",
            Modal = "Retira",
            NomeTransportadora = "Retirada Local",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            SepUser = "Vinicius",
            SepInicioData = DateTime.Now.AddMinutes(-20),
            EstimativaSolicitada = false,
            DataHoraEstimada = "11/10/2026 16:30",
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 3, CodItem = "003", Nome = "Bobina Plástico Bolha 1,20m x 100m", LocalPicking = "R3-C-10", Quantidade = 2 }
            }
        },

        // Pedido 3 - Aguardando Conferência
        new Pedido
        {
            IdPedido = 103,
            Cliente = "Vinicius",
            Modal = "Transportadora",
            NomeTransportadora = "Logística Rápida",
            CodTransportadora = "CTR-99",
            Status = "Aguardando Conferencia",
            Volumes = 0,
            SepUser = "Vinicius Amorim",
            SepFimData = DateTime.Now.AddHours(-1),
            EstimativaSolicitada = false,
            DataHoraEstimada = "11/10/2026 18:00",
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 2, CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", LocalPicking = "R2-A-05", Quantidade = 3 }
            }
        }
    };

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        await Task.Delay(150);
        return _pedidosMock;
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        await Task.Delay(150);
        return _pedidosMock.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<Pedido>> ObterPorClienteAsync(string termoCliente)
    {
        await Task.Delay(150);
        if (string.IsNullOrWhiteSpace(termoCliente)) return _pedidosMock;

        termoCliente = termoCliente.ToLower().Trim();

        return _pedidosMock.Where(p =>
            (!string.IsNullOrEmpty(p.Cliente) && p.Cliente.ToLower().Contains(termoCliente)) ||
            p.IdPedido.ToString() == termoCliente
        ).ToList();
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(int id)
    {
        await Task.Delay(100);
        return _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        await Task.Delay(100);
        return (true, string.Empty);
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(200);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        DateTime dataHoraAtual = DateTime.Now;

        if (mock.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Em Separação";
            mock.SepInicioData = dataHoraAtual;
        }
        else if (mock.Status.Equals("Em Separação", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Conferência";
            mock.SepUser = usuario ?? "Vinicius";
            mock.SepFimData = dataHoraAtual;
            mock.ErrosEvitados += errosEvitados;
        }
        else if (mock.Status.Equals("Aguardando Conferência", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Em Conferência";
            mock.ConfInicioData = dataHoraAtual;
        }
        else if (mock.Status.Equals("Aguardando Expedição", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Expedido";
            mock.ExpUser = usuario ?? "Vinicius";
            mock.ExpFimData = dataHoraAtual;
            mock.ErrosEvitados += errosEvitados;
        }

        return (true, "Sucesso");
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(200);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        mock.Status = "Aguardando Expedição";
        mock.Volumes = volumes;
        mock.ConfUser = usuario ?? "Vinicius";
        mock.ConfFimData = DateTime.Now;
        mock.ErrosEvitados += errosEvitados;

        mock.VolumesPedidos.Clear();
        for (int i = 1; i <= volumes; i++)
        {
            mock.VolumesPedidos.Add(new VolumePedidoDto
            {
                Id = i,
                CodigoVolume = $"c1p{mock.IdPedido}v{i}",
                BipadoExpedicao = false
            });
        }

        return (true, "Sucesso");
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        await Task.Delay(200);
        string fileName = $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        var linhasCsv = new List<string>
        {
            "ID Pedido;Cliente;Status;Sep User;Conf User;Exp User;Hora Inicio Separacao;Hora Fim Separacao;Hora Inicio Conferencia;Hora Fim Conferencia;Data Expedicao;Erros Evitados;Tempo Medio (min)"
        };

        foreach (var p in _pedidosMock)
        {
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
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        if (mock.Status.Equals("Em Separação", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando";
            mock.SepInicioData = null;
        }
        else if (mock.Status.Equals("Em Conferência", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Conferência";
            mock.ConfInicioData = null;
        }
        else if (mock.Status.Equals("Expedido", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Expedição";
        }

        return (true, "Sucesso");
    }

    public async Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada)
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        mock.DataHoraEstimada = dataHoraEstimada;
        mock.EstimativaSolicitada = false;

        return (true, string.Empty);
    }
}
