using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;
using SpringApp.Models;

namespace SpringApp.Services;

public class MockPedidoService : IPedidoService
{
    private static readonly List<Pedido> _historicoRelatorio = new List<Pedido>();

    private static List<ItemPedido> OrdenarRotaInteligente(IEnumerable<ItemPedido> itens)
    {
        return itens
            .OrderBy(i => i.IsCaixaFechada ? 1 : 0)
            .ThenBy(i => i.IsCaixaFechada ? i.LocalPulmao : i.LocalPicking, Comparer<string?>.Create(CompararEnderecoNatural))
            .ThenBy(i => i.CodItem, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int CompararEnderecoNatural(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (string.IsNullOrWhiteSpace(x)) return 1;
        if (string.IsNullOrWhiteSpace(y)) return -1;

        var tokensX = Regex.Matches(x.Trim().ToUpperInvariant(), @"(\d+|\D+)");
        var tokensY = Regex.Matches(y.Trim().ToUpperInvariant(), @"(\d+|\D+)");

        int limite = Math.Min(tokensX.Count, tokensY.Count);
        for (int i = 0; i < limite; i++)
        {
            string tx = tokensX[i].Value;
            string ty = tokensY[i].Value;

            bool numX = int.TryParse(tx, out int nx);
            bool numY = int.TryParse(ty, out int ny);

            int cmp = (numX && numY)
                ? nx.CompareTo(ny)
                : string.Compare(tx, ty, StringComparison.OrdinalIgnoreCase);

            if (cmp != 0) return cmp;
        }

        return tokensX.Count.CompareTo(tokensY.Count);
    }

    private static readonly List<Devolucao> _devolucoesMock = new List<Devolucao>
    {
        new Devolucao
        {
            Id = 1,
            PedidoId = 101,
            ClienteNome = "AcquAmorim (Pessoa Física)",
            DocumentoCliente = "389.451.220-10",
            EmiteNotaFiscal = false,
            CodigoAutorizacao = "DEV-0001",
            MotivoGeral = "Tamanho incompatível",
            Status = "Autorizada",
            DataSolicitacao = DateTime.Now.AddMinutes(-30),
            VolumesInformados = 1,
            Itens = new List<ItemDevolucao>
            {
                new ItemDevolucao { Id = 1, DevolucaoId = 1, CodItem = "001", Nome = "Caixa Organizadora Plástica 30L", QuantidadeSolicitada = 2 }
            }
        },
        new Devolucao
        {
            Id = 2,
            PedidoId = 102,
            ClienteNome = "Vinicius Comercial Ltda (Com NF)",
            DocumentoCliente = "12.345.678/0001-99",
            EmiteNotaFiscal = true,
            NumeroNotaDevolucao = "NF-98452",
            MotivoGeral = "Item enviado em duplicidade",
            Status = "Autorizada",
            DataSolicitacao = DateTime.Now.AddMinutes(-15),
            VolumesInformados = 1,
            Itens = new List<ItemDevolucao>
            {
                new ItemDevolucao { Id = 2, DevolucaoId = 2, CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", QuantidadeSolicitada = 3 }
            }
        }
    };

    private static readonly List<ProdutoAvariado> _avariadosMock = new List<ProdutoAvariado>
    {
        new ProdutoAvariado
        {
            Id = 1,
            CodItem = "001",
            Nome = "Caixa Organizadora Plástica 30L",
            Quantidade = 2,
            Origem = "Estoque Interno",
            MotivoAvaria = "Tampa trincada na prateleira R1-A-01",
            RegistradoPor = "Carlos Mendes",
            DataRegistro = DateTime.Now.AddHours(-2)
        }
    };

    private static readonly List<Cotacao> _cotacoesMock = new List<Cotacao>
    {
        new Cotacao
        {
            Id = 1,
            Cliente = "AcquAmorim",
            Observacao = "Caixa reforçada para transporte",
            Solicitante = "Comercial (Web)",
            DataSolicitacao = DateTime.Now.AddMinutes(-20),
            Status = "Pendente",
            Itens = new List<ItemCotacao>
            {
                new ItemCotacao { Id = 1, CodItem = "001", Nome = "Caixa Organizadora Plástica 30L", Quantidade = 4 },
                new ItemCotacao { Id = 2, CodItem = "002", Nome = "Fita Adesiva Transparente 45mm x 100m", Quantidade = 2 }
            }
        }
    };

    private static readonly List<Pedido> _pedidosMock = new List<Pedido>
    {
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
            EstimativaPendenteSeparacao = true,
            EstimativaPendenteConferencia = false,
            SolicitanteEstimativa = "Comercial (Web)",
            DataHoraEstimada = "",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 1,
                    PedidoId = 101,
                    ProdutoId = 1,
                    CodItem = "001",
                    CodCaixaFechada = "CX-001",
                    Nome = "Caixa Organizadora Plástica 30L",
                    LocalPicking = "R1-A-01",
                    LocalPulmao = "A1-P2",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 1,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                },
                new ItemPedido
                {
                    Id = 2,
                    PedidoId = 101,
                    ProdutoId = 1,
                    CodItem = "001",
                    CodCaixaFechada = "CX-001",
                    Nome = "Caixa Organizadora Plástica 30L (Caixa Fechada c/ 25 un.)",
                    LocalPicking = "R1-A-01",
                    LocalPulmao = "A1-P2",
                    IsCaixaFechada = true,
                    QtdPorVolume = 25,
                    TipoRota = 0,
                    Quantidade = 1,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                },
                new ItemPedido
                {
                    Id = 3,
                    PedidoId = 101,
                    ProdutoId = 2,
                    CodItem = "002",
                    CodCaixaFechada = "CX-002",
                    Nome = "Fita Adesiva Transparente 45mm x 100m",
                    LocalPicking = "R2-A-05",
                    LocalPulmao = "A1-P5",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 1,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                },
                new ItemPedido
                {
                    Id = 4,
                    PedidoId = 101,
                    ProdutoId = 3,
                    CodItem = "003",
                    Nome = "Bobina Plástico Bolha 1,20m x 100m",
                    LocalPicking = "LAB-TEC-01",
                    TipoRota = 1,
                    Quantidade = 1,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 102,
            Cliente = "Vinicius",
            Modal = "Retira",
            NomeTransportadora = "Retirada Local",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            EstimativaSolicitada = false,
            DataHoraEstimada = "11/10/2026 16:30",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 5,
                    PedidoId = 102,
                    ProdutoId = 3,
                    CodItem = "003",
                    Nome = "Bobina Plástico Bolha 1,20m x 100m",
                    LocalPicking = "LAB-TEC-01",
                    TipoRota = 1,
                    Quantidade = 2,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 103,
            Cliente = "Vinicius",
            Modal = "Transportadora",
            NomeTransportadora = "Logística Rápida",
            CodTransportadora = "CTR-99",
            Status = "Aguardando",
            Volumes = 0,
            EstimativaSolicitada = false,
            DataHoraEstimada = "11/10/2026 18:00",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 6,
                    PedidoId = 103,
                    ProdutoId = 2,
                    CodItem = "002",
                    CodCaixaFechada = "CX-002",
                    Nome = "Fita Adesiva Transparente 45mm x 100m",
                    LocalPicking = "R2-A-05",
                    LocalPulmao = "A1-P5",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 3,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 104,
            Cliente = "Distribuidora Serra Sul",
            Modal = "Transportadora",
            NomeTransportadora = "Expresso Paulista",
            CodTransportadora = "CTR-14",
            Status = "Aguardando",
            Volumes = 0,
            EstimativaSolicitada = true,
            EstimativaPendenteSeparacao = false,
            EstimativaPendenteConferencia = true,
            SolicitanteEstimativa = "Comercial (Web)",
            DataHoraEstimada = "21/11/2026 14:30",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 7,
                    PedidoId = 104,
                    ProdutoId = 1,
                    CodItem = "001",
                    CodCaixaFechada = "CX-001",
                    Nome = "Caixa Organizadora Plástica 30L",
                    LocalPicking = "R1-A-01",
                    LocalPulmao = "A1-P2",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 4,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                },
                new ItemPedido
                {
                    Id = 8,
                    PedidoId = 104,
                    ProdutoId = 2,
                    CodItem = "002",
                    CodCaixaFechada = "CX-002",
                    Nome = "Fita Adesiva Transparente 45mm x 100m (Caixa Fechada c/ 36 un.)",
                    LocalPicking = "R2-A-05",
                    LocalPulmao = "A1-P5",
                    IsCaixaFechada = true,
                    QtdPorVolume = 36,
                    TipoRota = 0,
                    Quantidade = 1,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 105,
            Cliente = "Mercado Central Itapecerica",
            Modal = "Loja",
            NomeTransportadora = "Cliente Loja",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            EstimativaSolicitada = false,
            DataHoraEstimada = "21/11/2026 15:45",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 9,
                    PedidoId = 105,
                    ProdutoId = 2,
                    CodItem = "002",
                    CodCaixaFechada = "CX-002",
                    Nome = "Fita Adesiva Transparente 45mm x 100m",
                    LocalPicking = "R2-A-05",
                    LocalPulmao = "A1-P5",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 6,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                },
                new ItemPedido
                {
                    Id = 10,
                    PedidoId = 105,
                    ProdutoId = 3,
                    CodItem = "003",
                    Nome = "Bobina Plástico Bolha 1,20m x 100m",
                    LocalPicking = "LAB-TEC-01",
                    TipoRota = 1,
                    Quantidade = 1,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 106,
            Cliente = "Constrular Horizonte",
            Modal = "Transportadora",
            NomeTransportadora = "Logística Rápida",
            CodTransportadora = "CTR-99",
            Status = "Aguardando Conferencia",
            Volumes = 2,
            EstimativaSolicitada = false,
            DataHoraEstimada = "21/11/2026 16:00",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 11,
                    PedidoId = 106,
                    ProdutoId = 1,
                    CodItem = "001",
                    CodCaixaFechada = "CX-001",
                    Nome = "Caixa Organizadora Plástica 30L (Caixa Fechada c/ 25 un.)",
                    LocalPicking = "R1-A-01",
                    LocalPulmao = "A1-P2",
                    IsCaixaFechada = true,
                    QtdPorVolume = 25,
                    TipoRota = 0,
                    Quantidade = 2,
                    BipagensSeparacao = 2,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 107,
            Cliente = "Atacado Bandeirantes",
            Modal = "Retira",
            NomeTransportadora = "Retirada Local",
            CodTransportadora = "CTR-00",
            Status = "Aguardando",
            Volumes = 0,
            EstimativaSolicitada = true,
            EstimativaPendenteSeparacao = true,
            EstimativaPendenteConferencia = false,
            SolicitanteEstimativa = "Comercial (Web)",
            DataHoraEstimada = "",
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 12,
                    PedidoId = 107,
                    ProdutoId = 1,
                    CodItem = "001",
                    CodCaixaFechada = "CX-001",
                    Nome = "Caixa Organizadora Plástica 30L",
                    LocalPicking = "R1-A-01",
                    LocalPulmao = "A1-P2",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 5,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                },
                new ItemPedido
                {
                    Id = 13,
                    PedidoId = 107,
                    ProdutoId = 3,
                    CodItem = "003",
                    Nome = "Bobina Plástico Bolha 1,20m x 100m",
                    LocalPicking = "LAB-TEC-02",
                    TipoRota = 1,
                    Quantidade = 3,
                    BipagensSeparacao = 0,
                    BipagensConferencia = 0
                }
            })
        },
        new Pedido
        {
            IdPedido = 108,
            Cliente = "Empório Embu Guaçu",
            Modal = "Transportadora",
            NomeTransportadora = "Expresso Paulista",
            CodTransportadora = "CTR-14",
            Status = "Aguardando Expediçao",
            Volumes = 2,
            EstimativaSolicitada = false,
            DataHoraEstimada = "21/11/2026 17:15",
            VolumesPedidos = new List<VolumePedidoDto>
            {
                new VolumePedidoDto { Id = 1, CodigoVolume = "c1p108v1", BipadoExpedicao = false },
                new VolumePedidoDto { Id = 2, CodigoVolume = "c1p108v2", BipadoExpedicao = false }
            },
            Itens = OrdenarRotaInteligente(new List<ItemPedido>
            {
                new ItemPedido
                {
                    Id = 14,
                    PedidoId = 108,
                    ProdutoId = 2,
                    CodItem = "002",
                    CodCaixaFechada = "CX-002",
                    Nome = "Fita Adesiva Transparente 45mm x 100m",
                    LocalPicking = "R2-A-05",
                    LocalPulmao = "A1-P5",
                    IsCaixaFechada = false,
                    QtdPorVolume = 1,
                    TipoRota = 0,
                    Quantidade = 4,
                    BipagensSeparacao = 4,
                    BipagensConferencia = 4
                }
            })
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
        var pedido = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (pedido != null && pedido.Itens != null)
        {
            pedido.Itens = OrdenarRotaInteligente(pedido.Itens);
        }
        return pedido;
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
            if (etapaAtual.Equals("Aguardando", StringComparison.OrdinalIgnoreCase))
                return (true, "Sucesso");

            if (mock.Itens != null && mock.Itens.Any(i => i.BipagensSeparacao < i.Quantidade))
            {
                mock.Status = "Em Separaçao";
                return (true, "Aguardando conclusão de todos os itens (incluindo Área Técnica).");
            }

            mock.Status = "Aguardando Conferencia";
            if (string.IsNullOrEmpty(mock.SepUser)) mock.SepUser = usuarioAtual;
            mock.SepFimData = dataHoraAtual;
            mock.ErrosEvitados += errosEvitados;
        }
        else if (mock.Status.Equals("Aguardando Conferencia", StringComparison.OrdinalIgnoreCase) || mock.Status.Equals("Aguardando Conferência", StringComparison.OrdinalIgnoreCase))
        {
            if (mock.Itens != null && mock.Itens.Any(i => i.BipagensSeparacao < i.Quantidade))
                return (false, "Ainda existem itens pendentes de separação (Normal ou Área Técnica).");

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

    public async Task<(bool Sucesso, string Mensagem)> ConcluirSeparacaoRotaAsync(Pedido pedido, int tipoRota, string usuario, int errosEvitados = 0)
    {
        await Task.Delay(200);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == pedido.IdPedido);
        if (mock == null) return (false, "Pedido não encontrado.");

        string usuarioAtual = ObterUsuarioLogado(usuario);
        mock.EmEspera = false;
        mock.EtapaPausada = string.Empty;
        mock.PausadoPor = string.Empty;
        mock.DataHoraPausa = null;
        mock.ErrosEvitados += errosEvitados;

        foreach (var itemMock in mock.Itens.Where(i => i.TipoRota == tipoRota))
        {
            var itemUi = pedido.Itens?.FirstOrDefault(x =>
                x.TipoRota == tipoRota &&
                x.IsCaixaFechada == itemMock.IsCaixaFechada &&
                x.CodItem.Equals(itemMock.CodItem, StringComparison.OrdinalIgnoreCase));

            int bipado = itemUi != null ? Math.Max(itemMock.BipagensSeparacao, itemUi.BipagensSeparacao) : itemMock.Quantidade;
            itemMock.BipagensSeparacao = Math.Min(itemMock.Quantidade, bipado);
        }

        if (string.IsNullOrWhiteSpace(mock.SepUser))
            mock.SepUser = usuarioAtual;
        else if (!mock.SepUser.Contains(usuarioAtual))
            mock.SepUser = $"{mock.SepUser} / {usuarioAtual}";

        bool todosConcluidos = mock.Itens.Any() && mock.Itens.All(i => i.BipagensSeparacao >= i.Quantidade);
        if (todosConcluidos)
        {
            mock.Status = "Aguardando Conferencia";
            mock.SepFimData = DateTime.Now;
        }
        else
        {
            mock.Status = "Em Separaçao";
        }

        return (true, "Sucesso");
    }

    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, int volumes, double peso = 0, string? observacao = null, string usuario = null, int errosEvitados = 0)
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
        mock.Peso = Math.Round(peso, 2);
        mock.ObservacaoConferencia = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
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

    public async Task<(bool Sucesso, string Mensagem)> PausarPedidoAsync(Pedido pedido, string etapa, string usuario, int errosEvitados = 0, string? motivo = null)
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == pedido.IdPedido);
        if (mock == null) return (false, "Pedido não encontrado.");

        string usuarioAtual = ObterUsuarioLogado(usuario);
        string motivoFinal = string.IsNullOrWhiteSpace(motivo) ? "Não informado" : motivo.Trim();
        DateTime agora = DateTime.Now;

        mock.EmEspera = true;
        mock.EtapaPausada = etapa;
        mock.PausadoPor = usuarioAtual;
        mock.DataHoraPausa = agora;
        mock.ErrosEvitados += errosEvitados;

        if (pedido.Itens != null)
        {
            foreach (var itemUi in pedido.Itens)
            {
                var itemMock = mock.Itens.FirstOrDefault(i =>
                    i.IsCaixaFechada == itemUi.IsCaixaFechada &&
                    i.CodItem.Equals(itemUi.CodItem, StringComparison.OrdinalIgnoreCase));

                if (itemMock != null)
                {
                    itemMock.BipagensSeparacao = Math.Max(itemMock.BipagensSeparacao, itemUi.BipagensSeparacao);
                    itemMock.BipagensConferencia = Math.Max(itemMock.BipagensConferencia, itemUi.BipagensConferencia);
                }
            }
        }

        if (etapa == "Expedição" && mock.ExpInicioData == null)
        {
            mock.ExpInicioData = agora;
            mock.ExpUser = usuarioAtual;
        }

        mock.HistoricoPausas.Add(new RegistroPausa
        {
            Etapa = etapa,
            OperadorPausa = usuarioAtual,
            DataHoraPausa = agora,
            Motivo = motivoFinal
        });

        return (true, "Pedido colocado em espera.");
    }

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

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id, string? motivo = null, string? usuario = null)
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        string usuarioAtual = ObterUsuarioLogado(usuario);
        string motivoFinal = string.IsNullOrWhiteSpace(motivo) ? "Não informado" : motivo.Trim();
        DateTime horaCancelamento = DateTime.Now;
        string statusAtual = mock.Status ?? "Aguardando";

        var pausaAberta = mock.HistoricoPausas.LastOrDefault(p => p.DataHoraRetomada == null);
        if (pausaAberta != null)
        {
            pausaAberta.OperadorRetomada = $"{usuarioAtual} (Cancelado: {motivoFinal})";
            pausaAberta.DataHoraRetomada = horaCancelamento;
        }

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

    public async Task<(bool Sucesso, string Mensagem)> CancelarPedidoAsync(int id, string usuario, string? motivo = null)
    {
        return await ReverterEtapaAsync(id, motivo, usuario);
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

    public async Task<(bool Sucesso, string Mensagem)> EnviarEstimativaAsync(int id, string dataHoraEstimada, string etapa = "", string usuario = "")
    {
        await Task.Delay(150);
        var mock = _pedidosMock.FirstOrDefault(p => p.IdPedido == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        string operador = ObterUsuarioLogado(usuario);
        bool respondeSep = etapa.Equals("Separacao", StringComparison.OrdinalIgnoreCase) ||
                           etapa.Equals("Separação", StringComparison.OrdinalIgnoreCase) ||
                           (string.IsNullOrEmpty(etapa) && mock.EstimativaPendenteSeparacao);

        if (respondeSep)
        {
            mock.EstimativaSeparacao = dataHoraEstimada;
            mock.EstimativaSeparacaoUser = operador;
            mock.EstimativaPendenteSeparacao = false;
            mock.EstimativaPendenteConferencia = true;
            mock.DataHoraEstimada = $"Sep: {dataHoraEstimada} ({operador})";
        }
        else
        {
            mock.EstimativaConferencia = dataHoraEstimada;
            mock.EstimativaConferenciaUser = operador;
            mock.EstimativaPendenteSeparacao = false;
            mock.EstimativaPendenteConferencia = false;
            mock.EstimativaSolicitada = false;
            mock.DataHoraEstimada = dataHoraEstimada;
        }

        return (true, string.Empty);
    }

    public async Task<List<Cotacao>> GetCotacoesPendentesAsync()
    {
        await Task.Delay(150);
        return _cotacoesMock
            .Where(c => c.Status.Equals("Pendente", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Id)
            .ToList();
    }

    public async Task<(bool Sucesso, string Mensagem)> ResponderCotacaoAsync(int id, int volumes, double pesoTotalKg, string dimensoes, string usuario)
    {
        await Task.Delay(150);
        var cotacao = _cotacoesMock.FirstOrDefault(c => c.Id == id);
        if (cotacao == null) return (false, "Cotação não encontrada.");

        cotacao.Volumes = volumes;
        cotacao.PesoTotalKg = Math.Round(pesoTotalKg, 2);
        cotacao.Dimensoes = dimensoes;
        cotacao.RespondidoPor = ObterUsuarioLogado(usuario);
        cotacao.Status = "Respondida";

        return (true, string.Empty);
    }

    public async Task<List<Devolucao>> GetDevolucoesAtivasAsync()
    {
        await Task.Delay(150);
        return _devolucoesMock.Where(d =>
            d.Status.Equals("Autorizada", StringComparison.OrdinalIgnoreCase) ||
            d.Status.Equals("Volumes Conferidos", StringComparison.OrdinalIgnoreCase) ||
            d.Status.Equals("Em Classificação", StringComparison.OrdinalIgnoreCase)
        ).OrderByDescending(d => d.Id).ToList();
    }

    public async Task<(bool Sucesso, string Mensagem, Devolucao? Devolucao)> ConferirVolumesDevolucaoAsync(int id, string codigoLido, int volumesRecebidos, string usuario)
    {
        await Task.Delay(150);
        var dev = _devolucoesMock.FirstOrDefault(d => d.Id == id);
        if (dev == null) return (false, "Devolução não encontrada.", null);

        string codigoEsperado = dev.EmiteNotaFiscal
            ? (dev.NumeroNotaDevolucao ?? string.Empty)
            : (dev.CodigoAutorizacao ?? string.Empty);

        if (string.IsNullOrWhiteSpace(codigoLido) || !codigoLido.Trim().Equals(codigoEsperado.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            string tipo = dev.EmiteNotaFiscal ? "NF de Devolução" : "Código de Autorização";
            return (false, $"{tipo} não confere com o cadastrado ('{codigoEsperado}'). Verifique e tente novamente ou clique em Recusar.", null);
        }

        if (volumesRecebidos <= 0)
            return (false, "Informe uma quantidade de volumes maior que zero.", null);

        dev.VolumesRecebidos = volumesRecebidos;
        dev.Status = "Volumes Conferidos";
        return (true, "Volumes conferidos!", dev);
    }

    public async Task<(bool Sucesso, bool AprovadaNaConferencia, string Mensagem, Devolucao? Devolucao)> ValidarItensDevolucaoAsync(int id, List<ItemBipadoDevolucaoInput> itensBipados, string usuario)
    {
        await Task.Delay(150);
        var dev = _devolucoesMock.FirstOrDefault(d => d.Id == id);
        if (dev == null) return (false, false, "Devolução não encontrada.", null);

        var divergencias = new List<string>();
        foreach (var item in dev.Itens)
        {
            var lido = itensBipados.FirstOrDefault(x => x.CodItem.Equals(item.CodItem, StringComparison.OrdinalIgnoreCase));
            int qtd = lido?.QuantidadeBipada ?? 0;
            item.QuantidadeBipada = qtd;

            if (qtd != item.QuantidadeSolicitada)
                divergencias.Add($"{item.Nome} (Esperado: {item.QuantidadeSolicitada}, Bipado: {qtd})");
        }

        if (divergencias.Any())
        {
            return (true, false, "Contagem divergente do formulário:\n• " + string.Join("\n• ", divergencias) + "\n\nCorrija a bipagem ou clique em 'Recusar'.", dev);
        }

        dev.Status = "Em Classificação";
        return (true, true, "Contagem conferida! Selecione o estado dos itens.", dev);
    }

    public async Task<(bool Sucesso, string Mensagem)> RecusarDevolucaoAsync(int id, string motivoRejeicao, string usuario)
    {
        await Task.Delay(150);
        var dev = _devolucoesMock.FirstOrDefault(d => d.Id == id);
        if (dev == null) return (false, "Devolução não encontrada.");

        if (string.IsNullOrWhiteSpace(motivoRejeicao))
            return (false, "Informe o motivo da recusa.");

        dev.Status = "Recusada na Doca";
        dev.MotivoRejeicao = motivoRejeicao.Trim();
        return (true, "Devolução recusada na doca.");
    }

    public async Task<(bool Sucesso, string Mensagem)> ClassificarItensDevolucaoAsync(int id, List<ItemClassificadoDevolucaoInput> itensClassificados, string usuario)
    {
        await Task.Delay(150);
        var dev = _devolucoesMock.FirstOrDefault(d => d.Id == id);
        if (dev == null) return (false, "Devolução não encontrada.");

        string op = ObterUsuarioLogado(usuario);

        foreach (var item in dev.Itens)
        {
            var c = itensClassificados.FirstOrDefault(x => x.CodItem.Equals(item.CodItem, StringComparison.OrdinalIgnoreCase));
            if (c == null || (c.QuantidadeBomEstado + c.QuantidadeAvariada) != item.QuantidadeSolicitada)
                return (false, $"A soma de Bom Estado + Avariado de '{item.Nome}' deve ser {item.QuantidadeSolicitada}.");

            item.QuantidadeBomEstado = c.QuantidadeBomEstado;
            item.QuantidadeAvariada = c.QuantidadeAvariada;

            if (c.QuantidadeAvariada > 0)
            {
                int novoAvId = _avariadosMock.Any() ? _avariadosMock.Max(a => a.Id) + 1 : 1;
                _avariadosMock.Insert(0, new ProdutoAvariado
                {
                    Id = novoAvId,
                    CodItem = item.CodItem,
                    Nome = item.Nome,
                    Quantidade = c.QuantidadeAvariada,
                    Origem = $"Devolução #{dev.Id:D4}",
                    DevolucaoId = dev.Id,
                    PedidoId = dev.PedidoId,
                    MotivoAvaria = string.IsNullOrWhiteSpace(c.MotivoAvaria) ? "Avaria constatada na triagem" : c.MotivoAvaria.Trim(),
                    RegistradoPor = op,
                    DataRegistro = DateTime.Now
                });
            }
        }

        dev.Status = "Concluida";
        return (true, "Devolução concluída com sucesso!");
    }

    public async Task<List<ProdutoAvariado>> GetProdutosAvariadosAsync()
    {
        await Task.Delay(150);
        return _avariadosMock.OrderByDescending(a => a.Id).ToList();
    }

    public async Task<(bool Sucesso, string Mensagem)> RegistrarAvariaEstoqueAsync(string codItem, int quantidade, string motivoAvaria, string usuario)
    {
        await Task.Delay(150);
        if (string.IsNullOrWhiteSpace(codItem) || quantidade <= 0)
            return (false, "Informe um SKU válido e quantidade maior que zero.");

        string sku = codItem.Trim();
        string nomeProduto = sku switch
        {
            "001" => "Caixa Organizadora Plástica 30L",
            "002" => "Fita Adesiva Transparente 45mm x 100m",
            "003" => "Bobina Plástico Bolha 1,20m x 100m",
            _ => $"Produto SKU {sku}"
        };

        int novoId = _avariadosMock.Any() ? _avariadosMock.Max(a => a.Id) + 1 : 1;
        _avariadosMock.Insert(0, new ProdutoAvariado
        {
            Id = novoId,
            CodItem = sku,
            Nome = nomeProduto,
            Quantidade = quantidade,
            Origem = "Estoque Interno",
            MotivoAvaria = string.IsNullOrWhiteSpace(motivoAvaria) ? "Avaria identificada no estoque" : motivoAvaria.Trim(),
            RegistradoPor = ObterUsuarioLogado(usuario),
            DataRegistro = DateTime.Now
        });

        return (true, "Produto avariado registrado na lista!");
    }
}