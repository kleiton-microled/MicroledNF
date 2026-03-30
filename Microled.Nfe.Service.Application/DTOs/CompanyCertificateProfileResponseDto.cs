namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Represents the persisted company profile associated with a certificate.
/// </summary>
public class CompanyCertificateProfileResponseDto
{
    public string Thumbprint { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public string MunicipalRegistration { get; set; } = string.Empty;

    public string? DefaultRemetenteCnpj { get; set; }

    public string? Environment { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }
}
