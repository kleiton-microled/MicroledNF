namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Response DTO for canceling NFe
/// </summary>
public class CancelNfeResponseDto
{
    public bool Sucesso { get; set; }
    public List<EventoDto> Alertas { get; set; } = new();
    public List<EventoDto> Erros { get; set; } = new();
}

