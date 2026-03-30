namespace Microled.Nfe.Service.Domain.Models;

/// <summary>
/// Stores the certificate selection and the company data associated with it.
/// </summary>
public class CompanyCertificateProfile
{
    public string Thumbprint { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string StoreLocation { get; set; } = "CurrentUser";

    public string StoreName { get; set; } = "My";

    public string CompanyName { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public string MunicipalRegistration { get; set; } = string.Empty;

    public string? DefaultRemetenteCnpj { get; set; }

    public string? Environment { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
