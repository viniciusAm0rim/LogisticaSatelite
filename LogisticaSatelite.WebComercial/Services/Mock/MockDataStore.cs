using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services.Mock;

public class MockDataStore
{
    public List<PedidoDto> Pedidos { get; }

    public MockDataStore()
    {
        var baseTime = DateTime.Today.AddHours(8);

        Pedidos = new List<PedidoDto>
        {
            CriarPedido(1024, "Construtora Horizonte Ltda", "Transportadora (Braspress)", "Aguardando", false,
                null, null, null, 0, 0, baseTime.AddHours(7), null, null, null, null, null, null),

            CriarPedido(1023, "Metalúrgica São Paulo S.A.", "Retira", "Em Separação", true,
                "Carlos Mendes", null, null, 1, 0, baseTime.AddHours(6.5), null, null, null, null, null, null),

            CriarPedido(1022, "Auto Peças Avenida", "Loja", "Aguardando Conferência", false,
                "Ana Paula Silva", null, null, 2, 0, baseTime.AddHours(6.0), baseTime.AddHours(6.3), null, null, null, null, null),

            CriarPedido(1021, "Indústria Mecânica Delta", "Transportadora (Jadlog)", "Em Conferência", true,
                "Rafael Costa", "Juliana Rocha", null, 1, 0, baseTime.AddHours(5.5), baseTime.AddHours(5.8), baseTime.AddHours(5.9), null, null, null, null),

            CriarPedido(1020, "Distribuidora Sulamericana", "Transportadora (TNT)", "Aguardando Expedição", false,
                "Carlos Mendes", "Ana Paula Silva", null, 3, 0, baseTime.AddHours(5.0), baseTime.AddHours(5.25), baseTime.AddHours(5.3), baseTime.AddHours(5.6), null, null, null,
                dataHoraEstimada: $"{DateTime.Today:dd/MM/yyyy} 16:30"),

            CriarPedido(1019, "Comercial Ferragens Centro", "Retira", "Expedido", false,
                "Juliana Rocha", "Rafael Costa", "Carlos Mendes", 2, 0, baseTime.AddHours(4.5), baseTime.AddHours(4.75), baseTime.AddHours(4.8), baseTime.AddHours(5.05), baseTime.AddHours(5.1), baseTime.AddHours(5.25),
                new RegistroPausaDto { Id = 1, Etapa = "Separação", OperadorPausa = "Juliana Rocha", DataHoraPausa = baseTime.AddHours(4.6), OperadorRetomada = "Juliana Rocha", DataHoraRetomada = baseTime.AddHours(4.68) }),

            CriarPedido(1018, "Engenharia & Obras Vértice", "Transportadora (Braspress)", "Expedido", false,
                "Ana Paula Silva", "Carlos Mendes", "Juliana Rocha", 4, 1, baseTime.AddHours(4.0), baseTime.AddHours(4.4), baseTime.AddHours(4.45), baseTime.AddHours(5.1), baseTime.AddHours(5.15), baseTime.AddHours(5.35),
                new RegistroPausaDto { Id = 2, Etapa = "Conferência", OperadorPausa = "Carlos Mendes", DataHoraPausa = baseTime.AddHours(4.7), OperadorRetomada = "Carlos Mendes", DataHoraRetomada = baseTime.AddHours(4.85) }),

            CriarPedido(1017, "Hidráulica Industrial Norte", "Loja", "Expedido", false,
                "Rafael Costa", "Juliana Rocha", "Ana Paula Silva", 1, 0, baseTime.AddHours(3.6), baseTime.AddHours(3.85), baseTime.AddHours(3.9), baseTime.AddHours(4.15), baseTime.AddHours(4.2), baseTime.AddHours(4.32)),

            CriarPedido(1016, "Serralheria Moderna Ltda", "Retira", "Expedido", false,
                "Carlos Mendes", "Rafael Costa", "Juliana Rocha", 2, 0, baseTime.AddHours(3.2), baseTime.AddHours(3.45), baseTime.AddHours(3.5), baseTime.AddHours(3.75), baseTime.AddHours(3.8), baseTime.AddHours(3.95)),

            CriarPedido(1015, "Agropecuária Boa Vista", "Transportadora (Rodonaves)", "Expedido", false,
                "Juliana Rocha", "Ana Paula Silva", "Rafael Costa", 3, 1, baseTime.AddHours(2.8), baseTime.AddHours(3.2), baseTime.AddHours(3.25), baseTime.AddHours(3.9), baseTime.AddHours(3.95), baseTime.AddHours(4.2),
                new RegistroPausaDto { Id = 3, Etapa = "Expedição", OperadorPausa = "Rafael Costa", DataHoraPausa = baseTime.AddHours(4.0), OperadorRetomada = "Rafael Costa", DataHoraRetomada = baseTime.AddHours(4.12) }),

            CriarPedido(1014, "Elétrica e Automação Paulista", "Transportadora (Jadlog)", "Expedido", false,
                "Ana Paula Silva", "Juliana Rocha", "Carlos Mendes", 2, 0, baseTime.AddHours(2.4), baseTime.AddHours(2.65), baseTime.AddHours(2.7), baseTime.AddHours(2.95), baseTime.AddHours(3.0), baseTime.AddHours(3.15)),

            CriarPedido(1013, "Manutenção Predial Aliança", "Loja", "Expedido", false,
                "Rafael Costa", "Carlos Mendes", "Ana Paula Silva", 1, 0, baseTime.AddHours(2.0), baseTime.AddHours(2.22), baseTime.AddHours(2.28), baseTime.AddHours(2.5), baseTime.AddHours(2.55), baseTime.AddHours(2.68)),

            CriarPedido(1012, "Transportes Rápido Express", "Retira", "Expedido", false,
                "Carlos Mendes", "Ana Paula Silva", "Rafael Costa", 3, 0, baseTime.AddHours(1.7), baseTime.AddHours(1.95), baseTime.AddHours(2.0), baseTime.AddHours(2.25), baseTime.AddHours(2.3), baseTime.AddHours(2.45)),

            CriarPedido(1011, "Usinagem Precisão Técnica", "Transportadora (TNT)", "Expedido", false,
                "Juliana Rocha", "Carlos Mendes", "Ana Paula Silva", 2, 1, baseTime.AddHours(1.4), baseTime.AddHours(1.7), baseTime.AddHours(1.75), baseTime.AddHours(2.15), baseTime.AddHours(2.2), baseTime.AddHours(2.35),
                new RegistroPausaDto { Id = 4, Etapa = "Separação", OperadorPausa = "Juliana Rocha", DataHoraPausa = baseTime.AddHours(1.5), OperadorRetomada = "Juliana Rocha", DataHoraRetomada = baseTime.AddHours(1.6) }),

            CriarPedido(1010, "Materiais de Construção Imperial", "Loja", "Expedido", false,
                "Ana Paula Silva", "Rafael Costa", "Juliana Rocha", 2, 0, baseTime.AddHours(1.1), baseTime.AddHours(1.35), baseTime.AddHours(1.4), baseTime.AddHours(1.65), baseTime.AddHours(1.7), baseTime.AddHours(1.85)),

            CriarPedido(1009, "Grupo Logístico Integrado", "Transportadora (Braspress)", "Expedido", false,
                "Rafael Costa", "Juliana Rocha", "Carlos Mendes", 4, 0, baseTime.AddHours(0.8), baseTime.AddHours(1.05), baseTime.AddHours(1.1), baseTime.AddHours(1.38), baseTime.AddHours(1.42), baseTime.AddHours(1.58)),

            CriarPedido(1008, "Refrigeração Comercial Sul", "Retira", "Expedido", false,
                "Carlos Mendes", "Rafael Costa", "Ana Paula Silva", 1, 0, baseTime.AddHours(0.5), baseTime.AddHours(0.75), baseTime.AddHours(0.8), baseTime.AddHours(1.05), baseTime.AddHours(1.1), baseTime.AddHours(1.22)),

            CriarPedido(1007, "Fábrica de Móveis Planejados", "Transportadora (Rodonaves)", "Expedido", false,
                "Juliana Rocha", "Ana Paula Silva", "Carlos Mendes", 2, 0, baseTime.AddHours(0.2), baseTime.AddHours(0.45), baseTime.AddHours(0.5), baseTime.AddHours(0.75), baseTime.AddHours(0.8), baseTime.AddHours(0.95)),

            CriarPedido(1006, "Atacadão das Ferramentas", "Loja", "Expedido", false,
                "Ana Paula Silva", "Carlos Mendes", "Rafael Costa", 3, 1, baseTime.AddHours(-0.3), baseTime.AddHours(0.05), baseTime.AddHours(0.1), baseTime.AddHours(0.45), baseTime.AddHours(0.5), baseTime.AddHours(0.68),
                new RegistroPausaDto { Id = 5, Etapa = "Conferência", OperadorPausa = "Carlos Mendes", DataHoraPausa = baseTime.AddHours(0.2), OperadorRetomada = "Carlos Mendes", DataHoraRetomada = baseTime.AddHours(0.32) }),

            CriarPedido(1005, "Indústria Química Bandeirantes", "Transportadora (TNT)", "Expedido", false,
                "Rafael Costa", "Ana Paula Silva", "Juliana Rocha", 2, 0, baseTime.AddHours(-0.7), baseTime.AddHours(-0.45), baseTime.AddHours(-0.4), baseTime.AddHours(-0.15), baseTime.AddHours(-0.1), baseTime.AddHours(0.05)),

            CriarPedido(1004, "Comércio de Parafusos União", "Retira", "Expedido", false,
                "Carlos Mendes", "Juliana Rocha", "Rafael Costa", 1, 0, baseTime.AddHours(-1.1), baseTime.AddHours(-0.85), baseTime.AddHours(-0.8), baseTime.AddHours(-0.55), baseTime.AddHours(-0.5), baseTime.AddHours(-0.35)),

            CriarPedido(1003, "Construtora Planalto Central", "Transportadora (Braspress)", "Expedido", false,
                "Juliana Rocha", "Rafael Costa", "Ana Paula Silva", 3, 0, baseTime.AddHours(-1.5), baseTime.AddHours(-1.25), baseTime.AddHours(-1.2), baseTime.AddHours(-0.92), baseTime.AddHours(-0.88), baseTime.AddHours(-0.72)),

            CriarPedido(1002, "Equipamentos Agrícolas Cerrado", "Transportadora (Jadlog)", "Expedido", false,
                "Ana Paula Silva", "Juliana Rocha", "Carlos Mendes", 2, 0, baseTime.AddHours(-1.9), baseTime.AddHours(-1.62), baseTime.AddHours(-1.58), baseTime.AddHours(-1.3), baseTime.AddHours(-1.25), baseTime.AddHours(-1.1)),

            CriarPedido(1001, "Supermercado Modelo Ltda", "Loja", "Expedido", false,
                "Rafael Costa", "Carlos Mendes", "Juliana Rocha", 1, 0, baseTime.AddHours(-2.3), baseTime.AddHours(-2.05), baseTime.AddHours(-2.0), baseTime.AddHours(-1.75), baseTime.AddHours(-1.7), baseTime.AddHours(-1.55))
        };
    }

    private static PedidoDto CriarPedido(
        int id,
        string destinatario,
        string modal,
        string status,
        bool estimativaSolicitada,
        string? sepUser,
        string? confUser,
        string? expUser,
        int errosEvitados,
        int errosRegistrados,
        DateTime? sepIni,
        DateTime? sepFim,
        DateTime? confIni,
        DateTime? confFim,
        DateTime? expIni,
        DateTime? expFim,
        RegistroPausaDto? pausa = null,
        string? dataHoraEstimada = null)
    {
        var pedido = new PedidoDto
        {
            Id = id,
            Destinatario = destinatario,
            Modal = modal,
            Status = status,
            EstimativaSolicitada = estimativaSolicitada,
            SolicitanteEstimativa = estimativaSolicitada ? "Comercial" : null,
            DataHoraEstimada = dataHoraEstimada,
            ErrosEvitados = errosEvitados,
            ErrosRegistrados = errosRegistrados,
            SepUser = sepUser,
            SepInicioData = sepIni,
            SepFimData = sepFim,
            ConfUser = confUser,
            ConfInicioData = confIni,
            ConfFimData = confFim,
            ExpUser = expUser,
            ExpInicioData = expIni,
            ExpFimData = expFim
        };

        if (pausa != null)
        {
            pedido.HistoricoPausas.Add(pausa);
        }

        return pedido;
    }
}
