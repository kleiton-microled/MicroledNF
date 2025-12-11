using System.Security.Cryptography.X509Certificates;
using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Domain.Interfaces;

/// <summary>
/// Service for generating NFe cancellation signature
/// </summary>
public interface INfeCancellationSignatureService
{
    /// <summary>
    /// Generates the signature string for NFe cancellation
    /// The string should contain 20 positions: InscricaoMunicipal (8) + NumeroNFe (12)
    /// Reference: NFe_Web_Service-4.pdf - Campos para assinatura de cancelamento
    /// </summary>
    string BuildSignatureString(NfeKey nfeKey);

    /// <summary>
    /// Signs the cancellation using the signature string and certificate
    /// Algorithm: SHA1 with RSA
    /// Returns Base64 encoded signature
    /// </summary>
    string SignCancellation(NfeKey nfeKey, X509Certificate2 certificate);
}

