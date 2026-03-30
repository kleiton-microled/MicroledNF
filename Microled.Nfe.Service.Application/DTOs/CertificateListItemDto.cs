namespace Microled.Nfe.Service.Application.DTOs;

/// <summary>
/// Represents a certificate available for manual selection.
/// </summary>
public class CertificateListItemDto
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

    public bool IsCurrentlySelected { get; set; }
}
