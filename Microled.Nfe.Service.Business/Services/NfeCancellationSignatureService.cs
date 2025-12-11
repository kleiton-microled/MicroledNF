using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Business.Services;

/// <summary>
/// Service for generating NFe cancellation signatures according to São Paulo City Hall specification
/// Reference: NFe_Web_Service-4.pdf - Campos para assinatura de cancelamento de NF-e
/// </summary>
public class NfeCancellationSignatureService : INfeCancellationSignatureService
{
    private readonly ILogger<NfeCancellationSignatureService> _logger;

    public NfeCancellationSignatureService(ILogger<NfeCancellationSignatureService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds the signature string for NFe cancellation (20 positions)
    /// Reference: NFe_Web_Service-4.pdf - Campos para assinatura de cancelamento
    /// Format: InscricaoMunicipal(8) + NumeroNFe(12)
    /// A cadeia de caracteres a ser assinada deverá conter 20 posições:
    /// - Inscrição Municipal (CCM) do Prestador com 8 caracteres (zeros à esquerda)
    /// - Número da NFS-e com 12 posições (zeros à esquerda)
    /// </summary>
    public string BuildSignatureString(NfeKey nfeKey)
    {
        if (nfeKey == null)
            throw new ArgumentNullException(nameof(nfeKey));

        // Inscrição Municipal (CCM) do Prestador com 8 caracteres (zeros à esquerda)
        // Referência: PDF - "Inscrição Municipal (CCM) do Prestador com 8 caracteres. Caso o CCM do Prestador tenha menos de 8 caracteres, o mesmo deverá ser completado com zeros à esquerda."
        var inscricaoMunicipal = nfeKey.InscricaoPrestador.ToString().PadLeft(8, '0');

        // Número da NFS-e com 12 posições (zeros à esquerda)
        // Referência: PDF - "Número da NFS-e RPS com 12 posições. Caso o Número da NFS-e tenha menos de 12 caracteres, o mesmo deverá ser completado com zeros à esquerda."
        var numeroNFe = nfeKey.NumeroNFe.ToString().PadLeft(12, '0');

        // Concatena os campos (total: 20 caracteres)
        // 8 + 12 = 20
        var signatureString = inscricaoMunicipal + numeroNFe;

        if (signatureString.Length != 20)
        {
            throw new InvalidOperationException($"Cancellation signature string must have exactly 20 characters, but has {signatureString.Length}");
        }

        return signatureString;
    }

    /// <summary>
    /// Signs the cancellation using SHA1 + RSA and returns Base64 encoded signature
    /// Reference: NFe_Web_Service-4.pdf - Algoritmo: SHA1 com RSA, resultado em Base64
    /// O certificado digital utilizado na assinatura de cancelamento deverá ser o mesmo utilizado na assinatura da mensagem XML.
    /// </summary>
    public string SignCancellation(NfeKey nfeKey, X509Certificate2 certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        var signatureString = BuildSignatureString(nfeKey);

        try
        {
            // Get RSA private key from certificate
            var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new InvalidOperationException("Certificate does not contain a private key. The certificate must have a private key for signing.");
            }

            // Convert signature string to bytes (ASCII encoding)
            var dataToSign = Encoding.ASCII.GetBytes(signatureString);

            // Sign using SHA1 with RSA and PKCS1 padding
            // Reference: PDF - Algoritmo SHA1 com RSA
            var signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

            // Convert to Base64
            var base64Signature = Convert.ToBase64String(signature);

            _logger.LogInformation("NFe cancellation signature generated successfully for NFe {InscricaoPrestador}-{NumeroNFe}",
                nfeKey.InscricaoPrestador, nfeKey.NumeroNFe);

            return base64Signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing NFe cancellation {InscricaoPrestador}-{NumeroNFe}",
                nfeKey.InscricaoPrestador, nfeKey.NumeroNFe);
            throw;
        }
    }
}

