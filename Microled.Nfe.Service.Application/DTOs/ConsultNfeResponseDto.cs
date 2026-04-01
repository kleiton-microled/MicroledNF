namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Response DTO for consulting NFe
/// </summary>
public class ConsultNfeResponseDto
{
    public bool Sucesso { get; set; }
    public List<NfeDto> NFeList { get; set; } = new();
    public List<EventoDto> Alertas { get; set; } = new();
    public List<EventoDto> Erros { get; set; } = new();
}

public class NfeDto
{
    public NfeKeyDto ChaveNFe { get; set; } = null!;
    public DateTime DataEmissao { get; set; }
    public DateTime DataFatoGerador { get; set; }
    public string Status { get; set; } = null!;
    public decimal ValorServicos { get; set; }
    public decimal ValorDeducoes { get; set; }
    public decimal ValorISS { get; set; }
    public string? CodigoVerificacao { get; set; }
    public string? NotaXml { get; set; }
}

