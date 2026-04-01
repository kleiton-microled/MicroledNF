namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request DTO for consulting async batch status by protocol number.
/// </summary>
public class ConsultBatchStatusRequestDto
{
    public string NumeroProtocolo { get; set; } = string.Empty;
}
