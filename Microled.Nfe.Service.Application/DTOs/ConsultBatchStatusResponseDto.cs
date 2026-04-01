namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Response DTO for consulting async batch status.
/// </summary>
public class ConsultBatchStatusResponseDto
{
    public bool Sucesso { get; set; }
    public int? SituacaoCodigo { get; set; }
    public string? SituacaoNome { get; set; }
    public long? NumeroLote { get; set; }
    public DateTime? DataRecebimento { get; set; }
    public DateTime? DataProcessamento { get; set; }
    public string? ResultadoOperacao { get; set; }
    public List<EventoDto> Erros { get; set; } = new();
}
