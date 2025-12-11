namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request DTO for canceling NFe
/// </summary>
public class CancelNfeRequestDto
{
    public NfeKeyDto ChaveNFe { get; set; } = null!;
    public bool Transacao { get; set; } = true;
}

