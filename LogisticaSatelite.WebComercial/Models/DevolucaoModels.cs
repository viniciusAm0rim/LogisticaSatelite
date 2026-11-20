namespace LogisticaSatelite.WebComercial.Models;

public class PedidoClienteDevolucaoDto
{
    public int PedidoId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public List<ItemDisponivelDevolucaoDto> Itens { get; set; } = new();
}

public class ItemDisponivelDevolucaoDto
{
    public string CodItem { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int QuantidadeComprada { get; set; }
    public int QuantidadeDisponivel { get; set; }
    public int QuantidadeSelecionada { get; set; }
}

public class CriarDevolucaoRequestDto
{
    public int PedidoId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string DocumentoCliente { get; set; } = string.Empty;
    public bool EmiteNotaFiscal { get; set; }
    public string? NumeroNotaDevolucao { get; set; }
    public string MotivoGeral { get; set; } = string.Empty;
    public int VolumesInformados { get; set; } = 1;
    public List<ItemDevolucaoInputDto> Itens { get; set; } = new();
}

public class ItemDevolucaoInputDto
{
    public string CodItem { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class DevolucaoDto
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string DocumentoCliente { get; set; } = string.Empty;
    public bool EmiteNotaFiscal { get; set; }
    public string? NumeroNotaDevolucao { get; set; }
    public string? CodigoAutorizacao { get; set; }
    public string MotivoGeral { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendente Analise";
    public DateTime DataSolicitacao { get; set; }
    public string? AnalisadoPor { get; set; }
    public DateTime? DataAnalise { get; set; }
    public string? MotivoRejeicao { get; set; }
    public int VolumesInformados { get; set; } = 1;
    public int? VolumesRecebidos { get; set; }
    public string? ConferidoPor { get; set; }
    public DateTime? DataConferencia { get; set; }
    public List<ItemDevolucaoDto> Itens { get; set; } = new();
}

public class ItemDevolucaoDto
{
    public int Id { get; set; }
    public int DevolucaoId { get; set; }
    public string CodItem { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int QuantidadeSolicitada { get; set; }
    public int QuantidadeBipada { get; set; }
    public int QuantidadeBomEstado { get; set; }
    public int QuantidadeAvariada { get; set; }
    public string? MotivoAvaria { get; set; }
}

public class ProdutoAvariadoDto
{
    public int Id { get; set; }
    public string CodItem { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public string Origem { get; set; } = "Estoque Interno";
    public int? DevolucaoId { get; set; }
    public int? PedidoId { get; set; }
    public string MotivoAvaria { get; set; } = string.Empty;
    public string RegistradoPor { get; set; } = string.Empty;
    public DateTime DataRegistro { get; set; }
}