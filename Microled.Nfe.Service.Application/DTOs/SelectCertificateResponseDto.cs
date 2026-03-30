namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Result of selecting the active certificate.
/// </summary>
public class SelectCertificateResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Thumbprint { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
}
