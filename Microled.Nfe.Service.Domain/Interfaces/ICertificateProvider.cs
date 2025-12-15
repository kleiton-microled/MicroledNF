using System.Security.Cryptography.X509Certificates;

namespace Microled.Nfe.Service.Domain.Interfaces;

/// <summary>
/// Service for providing digital certificates
/// </summary>
public interface ICertificateProvider
{
    /// <summary>
    /// Gets the certificate for signing
    /// </summary>
    X509Certificate2 GetCertificate();
}

