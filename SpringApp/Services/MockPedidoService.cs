using System.Globalization;
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
        // Pedidos em espera ficam visíveis apenas no card superior dedicado até serem retomados
        return _pedidosMock
            .Where(p => !p.EmEspera && p.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .ToList();
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

        mock.EmEspera = false;
        mock.EtapaPausada = string.Empty;
        mock.PausadoPor = string.Empty;
        mock.DataHoraPausa = null;

        if (mock.Status.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Em Separaçao";
            mock.SepUser = usuarioAtual;
            mock.SepInicioData ??= dataHoraAtual;
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
            mock.ConfInicioData ??= dataHoraAtual;
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

        mock.EmEspera = false;
        mock.EtapaPausada = string.Empty;
        mock.PausadoPor = string.Empty;
        mock.DataHoraPausa = null;

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
    /// Coloca o pedido em espera (pausa), preservando os itens/volumes já bipados e registrando o evento para o relatório.
    /// </summary>
    public async Task<(bool Sucesso, string Mensagem)> PausarPedidoAsync(Pedido pedido, string etapa, string usuario, int errosEvitados = 0)
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == pedido.IdPedido);
        if (mock == null) return (false, "Pedido não encontrado.");

        string usuarioAtual = ObterUsuarioLogado(usuario);
        DateTime agora = DateTime.Now;

        mock.EmEspera = true;
        mock.EtapaPausada = etapa;
        mock.PausadoPor = usuarioAtual;
        mock.DataHoraPausa = agora;
        mock.ErrosEvitados += errosEvitados;

        if (etapa == "Expedição" && mock.ExpInicioData == null)
        {
            mock.ExpInicioData = agora;
            mock.ExpUser = usuarioAtual;
        }

        mock.HistoricoPausas.Add(new RegistroPausa
        {
            Etapa = etapa,
            OperadorPausa = usuarioAtual,
            DataHoraPausa = agora
        });

        return (true, "Pedido colocado em espera.");
    }

    /// <summary>
    /// Retoma um pedido que estava em espera, registrando a retomada para o relatório e mantendo o progresso salvo.
    /// </summary>
    public async Task<(bool Sucesso, string Mensagem)> RetomarPedidoAsync(int id, string usuario)
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        string usuarioAtual = ObterUsuarioLogado(usuario);
        DateTime agora = DateTime.Now;

        var ultimaPausaAberta = mock.HistoricoPausas.LastOrDefault(p => p.DataHoraRetomada == null);
        if (ultimaPausaAberta != null)
        {
            ultimaPausaAberta.OperadorRetomada = usuarioAtual;
            ultimaPausaAberta.DataHoraRetomada = agora;
        }

        mock.EmEspera = false;
        mock.PausadoPor = string.Empty;
        mock.DataHoraPausa = null;

        return (true, "Pedido retomado.");
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

        // Encerra eventual pausa aberta caso o operador cancele diretamente
        var pausaAberta = mock.HistoricoPausas.LastOrDefault(p => p.DataHoraRetomada == null);
        if (pausaAberta != null)
        {
            pausaAberta.OperadorRetomada = $"{usuarioAtual} (Cancelado)";
            pausaAberta.DataHoraRetomada = horaCancelamento;
        }

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
                ErrosEvitados = mock.ErrosEvitados,
                HistoricoPausas = new List<RegistroPausa>(mock.HistoricoPausas)
            };

            _historicoRelatorio.Add(registroCancelado);
        }

        mock.EmEspera = false;
        mock.EtapaPausada = string.Empty;
        mock.PausadoPor = string.Empty;
        mock.DataHoraPausa = null;

        // 2. REVERTE O STATUS DO PEDIDO E RESETA AS BIPAGENS DOS ITENS
        if (statusAtual.Equals("Em Separaçao", StringComparison.OrdinalIgnoreCase) ||
            statusAtual.Equals("Em Separação", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando";
            mock.SepUser = null;
            mock.SepInicioData = null;
            mock.SepFimData = null;

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

            if (mock.Itens != null)
            {
                foreach (var item in mock.Itens)
                {
                    item.BipagensConferencia = 0;
                }
            }
        }
        else if (statusAtual.Equals("Aguardando Expediçao", StringComparison.OrdinalIgnoreCase) ||
                 statusAtual.Equals("Expedido", StringComparison.OrdinalIgnoreCase))
        {
            mock.Status = "Aguardando Expediçao";
            mock.ExpUser = null;
            mock.ExpFimData = null;

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
            "ID Pedido;Cliente;Status;Sep User;Conf User;Exp User;Hora Inicio Separacao;Hora Fim Separacao;Hora Inicio Conferencia;Hora Fim Conferencia;Data Expedicao;Hora Cancelamento;Erros Evitados;Qtd Pausas;Tempo Pausado (min);Historico Pausas e Retomadas;Tempo Medio (min)"
        };

        var todosRegistros = _historicoRelatorio.Concat(_pedidosMock).ToList();

        foreach (var p in todosRegistros)
        {
            if (!string.IsNullOrWhiteSpace(operadorFiltro) && !operadorFiltro.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                bool matchSep = p.SepUser != null && p.SepUser.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase);
                bool matchConf = p.ConfUser != null && p.ConfUser.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase);
                bool matchExp = p.ExpUser != null && p.ExpUser.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase);
                bool matchPausa = p.HistoricoPausas != null && p.HistoricoPausas.Any(h =>
                    (h.OperadorPausa != null && h.OperadorPausa.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase)) ||
                    (h.OperadorRetomada != null && h.OperadorRetomada.Equals(operadorFiltro, StringComparison.OrdinalIgnoreCase)));

                if (!matchSep && !matchConf && !matchExp && !matchPausa)
                    continue;
            }

            string statusExibicao = p.EmEspera ? $"Em Espera ({p.EtapaPausada})" : p.Status;
            string sepInicio = p.SepInicioData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string sepFim = p.SepFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string confInicio = p.ConfInicioData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string confFim = p.ConfFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string expFim = p.ExpFimData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
            string cancelData = p.CancelData?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

            int qtdPausas = p.HistoricoPausas?.Count ?? 0;
            double minutosPausados = 0;
            string detalhesPausas = "-";

            if (qtdPausas > 0 && p.HistoricoPausas != null)
            {
                var eventosFormatados = new List<string>();
                foreach (var h in p.HistoricoPausas)
                {
                    DateTime fimRef = h.DataHoraRetomada ?? DateTime.Now;
                    minutosPausados += Math.Max(0, (fimRef - h.DataHoraPausa).TotalMinutes);

                    string retomadaStr = h.DataHoraRetomada.HasValue
                        ? $"Retomado {h.DataHoraRetomada.Value:HH:mm:ss} ({h.OperadorRetomada})"
                        : "Ainda em espera";

                    eventosFormatados.Add($"[{h.Etapa}] Pausa {h.DataHoraPausa:HH:mm:ss} ({h.OperadorPausa}) -> {retomadaStr}");
                }
                detalhesPausas = string.Join(" | ", eventosFormatados);
            }

            string tempoPausadoStr = minutosPausados.ToString("F2", new CultureInfo("pt-BR"));

            linhasCsv.Add($"{p.IdPedido};{p.Cliente};{statusExibicao};{p.SepUser ?? "-"};{p.ConfUser ?? "-"};{p.ExpUser ?? "-"};{sepInicio};{sepFim};{confInicio};{confFim};{expFim};{cancelData};{p.ErrosEvitados};{qtdPausas};{tempoPausadoStr};{detalhesPausas};5,00");
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