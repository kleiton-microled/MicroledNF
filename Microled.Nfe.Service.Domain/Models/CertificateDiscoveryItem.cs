namespace Microled.Nfe.Service.Domain.Models;

/// <summary>
/// Represents a certificate discovered in the local machine stores.
/// </summary>
public class CertificateDiscoveryItem
{
    public string Thumbprint { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public DateTime NotBefore { get; set; }

    public DateTime NotAfter { get; set; }

    public bool HasPrivateKey { get; set; }

    public string StoreLocation { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string? SimpleName { get; set; }

    public string? Cnpj { get; set; }

    public string? Cpf { get; set; }

    public bool IsA3Candidate { get; set; }
}
