namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request DTO for consulting NFe
/// </summary>
public class ConsultNfeRequestDto
{
    public NfeKeyDto? ChaveNFe { get; set; }
    public RpsKeyDto? ChaveRps { get; set; }
}

