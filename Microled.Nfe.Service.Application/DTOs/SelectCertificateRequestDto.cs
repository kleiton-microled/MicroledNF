namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request used to select the active certificate.
/// </summary>
public class SelectCertificateRequestDto
{
    public string Thumbprint { get; set; } = string.Empty;

    public string StoreLocation { get; set; } = "CurrentUser";

    public string StoreName { get; set; } = "My";
}
