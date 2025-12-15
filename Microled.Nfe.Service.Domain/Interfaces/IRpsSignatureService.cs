using System.Security.Cryptography.X509Certificates;
using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Domain.Interfaces;

/// <summary>
/// Service for generating RPS digital signature
/// </summary>
public interface IRpsSignatureService
{
    /// <summary>
    /// Generates the signature string for RPS according to the official specification
    /// The string should contain 86 positions with specific fields and formatting
    /// Reference: NFe_Web_Service-4.pdf - Campos para assinatura do RPS (versão 2.0)
    /// </summary>
    string BuildSignatureString(Rps rps);

    /// <summary>
    /// Signs the RPS using the signature string and certificate
    /// Algorithm: SHA1 with RSA
    /// Returns Base64 encoded signature
    /// </summary>
    string SignRps(Rps rps, X509Certificate2 certificate);
}

