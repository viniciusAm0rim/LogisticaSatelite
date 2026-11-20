using System.Text.Json.Serialization;

namespace SpringApp.Models;

public class Devolucao
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("pedidoId")]
    public int PedidoId { get; set; }

    [JsonPropertyName("clienteNome")]
    public string ClienteNome { get; set; } = string.Empty;

    [JsonPropertyName("documentoCliente")]
    public string DocumentoCliente { get; set; } = string.Empty;

    [JsonPropertyName("emiteNotaFiscal")]
    public bool EmiteNotaFiscal { get; set; }

    [JsonPropertyName("numeroNotaDevolucao")]
    public string? NumeroNotaDevolucao { get; set; }

    [JsonPropertyName("codigoAutorizacao")]
    public string? CodigoAutorizacao { get; set; }

    [JsonPropertyName("motivoGeral")]
    public string MotivoGeral { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Autorizada";

    [JsonPropertyName("dataSolicitacao")]
    public DateTime DataSolicitacao { get; set; }

    [JsonPropertyName("volumesInformados")]
    public int VolumesInformados { get; set; } = 1;

    [JsonPropertyName("volumesRecebidos")]
    public int? VolumesRecebidos { get; set; }

    [JsonPropertyName("motivoRejeicao")]
    public string? MotivoRejeicao { get; set; }

    [JsonPropertyName("itens")]
    public List<ItemDevolucao> Itens { get; set; } = new();

    [JsonIgnore]
    public string DocumentoIdentificacaoLabel => EmiteNotaFiscal
        ? $"NF Devolução: {NumeroNotaDevolucao}"
        : $"Cód. Autorização (PF): {CodigoAutorizacao}";

    [JsonIgnore]
    public string TextoBotaoAcao => Status switch
    {
        "Autorizada" => "Conferir Volumes",
        "Volumes Conferidos" => "Bipar Itens",
        "Em Classificação" => "Classificar Itens",
        _ => "Visualizar"
    };
}

public class ItemDevolucao
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("devolucaoId")]
    public int DevolucaoId { get; set; }

    [JsonPropertyName("codItem")]
    public string CodItem { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("quantidadeSolicitada")]
    public int QuantidadeSolicitada { get; set; }

    [JsonPropertyName("quantidadeBipada")]
    public int QuantidadeBipada { get; set; }

    [JsonPropertyName("quantidadeBomEstado")]
    public int QuantidadeBomEstado { get; set; }

    [JsonPropertyName("quantidadeAvariada")]
    public int QuantidadeAvariada { get; set; }

    [JsonPropertyName("motivoAvaria")]
    public string? MotivoAvaria { get; set; }
}

public class ProdutoAvariado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codItem")]
    public string CodItem { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }

    [JsonPropertyName("origem")]
    public string Origem { get; set; } = "Estoque Interno";

    [JsonPropertyName("devolucaoId")]
    public int? DevolucaoId { get; set; }

    [JsonPropertyName("pedidoId")]
    public int? PedidoId { get; set; }

    [JsonPropertyName("motivoAvaria")]
    public string MotivoAvaria { get; set; } = string.Empty;

    [JsonPropertyName("registradoPor")]
    public string RegistradoPor { get; set; } = string.Empty;

    [JsonPropertyName("dataRegistro")]
    public DateTime DataRegistro { get; set; }
}

public class ItemBipadoDevolucaoInput
{
    public string CodItem { get; set; } = string.Empty;
    public int QuantidadeBipada { get; set; }
}

public class ItemClassificadoDevolucaoInput
{
    public string CodItem { get; set; } = string.Empty;
    public int QuantidadeBomEstado { get; set; }
    public int QuantidadeAvariada { get; set; }
    public string? MotivoAvaria { get; set; }
}