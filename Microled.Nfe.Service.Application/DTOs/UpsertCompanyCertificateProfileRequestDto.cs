namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Request used to create or update the company data linked to a certificate.
/// </summary>
public class UpsertCompanyCertificateProfileRequestDto
{
    public string Thumbprint { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public string MunicipalRegistration { get; set; } = string.Empty;

    public string? DefaultRemetenteCnpj { get; set; }

    public string? Environment { get; set; }

    public string? Notes { get; set; }
}
