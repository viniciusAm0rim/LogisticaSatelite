using Microsoft.Maui.Storage;
using SpringApp.Models;

namespace SpringApp.Services;

public class MockPedidoService : IPedidoService
{
    // Histórico acumulativo para registrar tentativas e cancelamentos em linhas separadas
    private static readonly List<Pedido> _historicoRelatorio = new List<Pedido>();

    // Lista principal de pedidos do ambiente mock
    private static readonly List<Pedido> _pedidosMock = new List<Pedido>
    {
        // Pedido 101 - Aguardando
        new Pedido
        {
            IdPedido = 101,
            Cliente = "AcquAmorim",
            Modal = "Loja",
            NomeTransportadora = "Cliente Loja",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            SepUser = null,
            SepInicioData = null,
            SepFimData = null,
            ConfUser = null,
            ConfInicioData = null,
            ConfFimData = null,
            ExpUser = null,
            ExpInicioData = null,
            ExpFimData = null,
            CancelData = null,
            EstimativaSolicitada = true,
            SolicitanteEstimativa = "Comercial (Web)",
            DataHoraEstimada = "",
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 1, CodItem = "001", Nome = "Caixa Organizadora Plástica 30L", LocalPicking = "R1-A-01", Quantidade = 1, BipagensSeparacao = 0, BipagensConferencia = 0 },
                new ItemPedido { ProdutoId = 2, CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", LocalPicking = "R2-A-05", Quantidade = 1, BipagensSeparacao = 0, BipagensConferencia = 0 }
            }
        },

        // Pedido 102 - Aguardando
        new Pedido
        {
            IdPedido = 102,
            Cliente = "Vinicius",
            Modal = "Retira",
            NomeTransportadora = "Retirada Local",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            SepUser = null,
            SepInicioData = null,
            SepFimData = null,
            ConfUser = null,
            ConfInicioData = null,
            ConfFimData = null,
            ExpUser = null,
            ExpInicioData = null,
            ExpFimData = null,
            CancelData = null,
            EstimativaSolicitada = false,
            DataHoraEstimada = "11/10/2026 16:30",
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 3, CodItem = "003", Nome = "Bobina Plástico Bolha 1,20m x 100m", LocalPicking = "R3-C-10", Quantidade = 2, BipagensSeparacao = 0, BipagensConferencia = 0 }
            }
        },

        // Pedido 103 - Aguardando
        new Pedido
        {
            IdPedido = 103,
            Cliente = "Vinicius",
            Modal = "Transportadora",
            NomeTransportadora = "Logística Rápida",
            CodTransportadora = "CTR-99",
            Status = "Aguardando",
            Volumes = 0,
            SepUser = null,
            SepInicioData = null,
            SepFimData = null,
            ConfUser = null,
            ConfInicioData = null,
            ConfFimData = null,
            ExpUser = null,
            ExpInicioData = null,
            ExpFimData = null,
            CancelData = null,
            EstimativaSolicitada = false,
            DataHoraEstimada = "11/10/2026 18:00",
            Itens = new List<ItemPedido>
            {
                new ItemPedido { ProdutoId = 2, CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", LocalPicking = "R2-A-05", Quantidade = 3, BipagensSeparacao = 0, BipagensConferencia = 0 }
            }
        }
    };

    private static string ObterUsuarioLogado(string? usuarioParametro)
    {
        if (!string.IsNullOrWhiteSpace(usuarioParametro))
            return usuarioParametro;

        string usuarioNome = Preferences.Get("UsuarioNome", string.Empty);
        if (!string.IsNullOrWhiteSpace(usuarioNome))
            return usuarioNome;

        string usuarioUser = Preferences.Get("UsuarioUser", string.Empty);
        if (!string.IsNullOrWhiteSpace(usuarioUser))
            return usuarioUser;

        return "Supervisor";
    }

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
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        string usuarioAtual = ObterUsuarioLogado(usuario);

        if (mock != null)
        {
            if (string.IsNullOrEmpty(mock.SepUser)) mock.SepUser = usuarioAtual;
        }
        return (true, string.Empty);
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null, int errosEvitados = 0)
    {
        await Task.Delay(200);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        DateTime dataHoraAtual = DateTime.Now;
        string usuarioAtual = ObterUsuarioLogado(usuario);

        if (mock.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Em Separaçao";
            mock.SepUser = usuarioAtual;
            mock.SepInicioData = dataHoraAtual;
        }
        else if (mock.Status.Equals("Em Separaçao", StringComparison.OrdinalIgnoreCase) || mock.Status.Equals("Em Separação", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Conferencia";
            if (string.IsNullOrEmpty(mock.SepUser)) mock.SepUser = usuarioAtual;
            mock.SepFimData = dataHoraAtual;
            mock.ErrosEvitados += errosEvitados;
        }
        else if (mock.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase) || mock.Status.Equals("Aguardando Conferência", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Em Conferencia";
            mock.ConfUser = usuarioAtual;
            mock.ConfInicioData = dataHoraAtual;
        }
        else if (mock.Status.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase) || mock.Status.Equals("Aguardando Expedição", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Expedido";
            mock.ExpUser = usuarioAtual;
            mock.ExpInicioData ??= dataHoraAtual;
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

        string usuarioAtual = ObterUsuarioLogado(usuario);

        mock.Status = "Aguardando Expediçao";
        mock.Volumes = volumes;
        mock.ConfUser = usuarioAtual;
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

    /// <summary>
    /// Método acionado pelo botão 'Cancelar' das páginas.
    /// Registra a tentativa cancelada em uma linha isolada no relatório e reverte o pedido + itens lidos.
    /// </summary>
    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id)
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        string usuarioAtual = ObterUsuarioLogado(null);
        DateTime horaCancelamento = DateTime.Now;
        string statusAtual = mock.Status ?? "Aguardando";

        // 1. SE O PEDIDO ESTAVA EM ANDAMENTO, GRAVA UMA LINHA DE CANCELAMENTO NO HISTÓRICO
        if (!statusAtual.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
        {
            var registroCancelado = new Pedido
            {
                IdPedido = mock.IdPedido,
                Cliente = mock.Cliente,
                Modal = mock.Modal,
                Status = "Cancelado",
                SepUser = statusAtual.Contains("Separa", StringComparison.OrdinalIgnoreCase) ? (mock.SepUser ?? usuarioAtual) : mock.SepUser,
                ConfUser = statusAtual.Contains("Conferen", StringComparison.OrdinalIgnoreCase) ? (mock.ConfUser ?? usuarioAtual) : mock.ConfUser,
                ExpUser = statusAtual.Contains("Exped", StringComparison.OrdinalIgnoreCase) ? (mock.ExpUser ?? usuarioAtual) : mock.ExpUser,
                SepInicioData = mock.SepInicioData,
                SepFimData = mock.SepFimData,
                ConfInicioData = mock.ConfInicioData,
                ConfFimData = mock.ConfFimData,
                CancelData = horaCancelamento,
                ErrosEvitados = mock.ErrosEvitados
            };

            _historicoRelatorio.Add(registroCancelado);
        }

        // 2. REVERTE O STATUS DO PEDIDO E RESETA AS BIPAGENS DOS ITENS
        if (statusAtual.Equals("Em Separaçao", StringComparison.OrdinalIgnoreCase) ||
            statusAtual.Equals("Em Separação", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando";
            mock.SepUser = null;
            mock.SepInicioData = null;
            mock.SepFimData = null;

            // Zera as leituras de separação dos itens
            if (mock.Itens != null)
            {
                foreach (var item in mock.Itens)
                {
                    item.BipagensSeparacao = 0;
                }
            }
        }
        else if (statusAtual.Equals("Em Conferencia", StringComparison.OrdinalIgnoreCase) ||
                 statusAtual.Equals("Em Conferência", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Conferencia";
            mock.ConfUser = null;
            mock.ConfInicioData = null;
            mock.ConfFimData = null;

            // Zera as leituras de conferência dos itens
            if (mock.Itens != null)
            {
                foreach (var item in mock.Itens)
                {
                    item.BipagensConferencia = 0;
                }
            }
        }
        else if (statusAtual.Equals("Expedido", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Expediçao";
            mock.ExpUser = null;
            mock.ExpFimData = null;

            // Zera as bipagens de volumes se houver
            if (mock.VolumesPedidos != null)
            {
                foreach (var vol in mock.VolumesPedidos)
                {
                    vol.BipadoExpedicao = false;
                }
            }
        }
        else
        {
            mock.Status = "Aguardando";
        }

        mock.CancelData = horaCancelamento;

        return (true, "Sucesso");
    }

    public async Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario)
    {
        return await ReverterEtapaAsync(id);
    }

    public async Task<string?> BaixarRelatorioProdutividadeAsync(string operadorFiltro)
    {
        await Task.Delay(200);
        string fileName = $"Relatorio_Produtividade_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        var linhasCsv = new List<string>
        {
            "ID Pedido;Cliente;Status;Sep User;Conf User;Exp User;Hora Inicio Separacao;Hora Fim Separacao;Hora Inicio Conferencia;Hora Fim Conferencia;Data Expedicao;Hora Cancelamento;Erros Evitados;Tempo Medio (min)"
        };

        // Consolida o histórico de cancelamentos com todos os pedidos atuais
        var todosRegistros = _historicoRelatorio.Concat(_pedidosMock).ToList();

        foreach (var p in todosRegistros)
        {
            if (!string.IsNullOrWhiteSpace(operadorFiltro) && !operadorFiltro.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                bool matchSep = p.SepUser != null && p.SepUser.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase);
                bool matchConf = p.ConfUser != null && p.ConfUser.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase);
                bool matchExp = p.ExpUser != null && p.ExpUser.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase);

                if (!matchSep && !matchConf && !matchExp)
                    continue;
            }

            string sepInicio = p.SepInicioData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string sepFim = p.SepFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string confInicio = p.ConfInicioData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string confFim = p.ConfFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string expFim = p.ExpFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string cancelData = p.CancelData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

            linhasCsv.Add($"{p.IdPedido};{p.Cliente};{p.Status};{p.SepUser ?? "-"};{p.ConfUser ?? "-"};{p.ExpUser ?? "-"};{sepInicio};{sepFim};{confInicio};{confFim};{expFim};{cancelData};{p.ErrosEvitados};5,00");
        }

        await File.WriteAllLinesAsync(filePath, linhasCsv, System.Text.Encoding.UTF8);
        return filePath;
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