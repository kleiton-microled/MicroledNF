using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microled.Nfe.LocalAgent.Api.Contracts;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.LocalAgent.Api.Services;

public class CertificateUnlockService
{
    private static readonly byte[] ProbePayload = "Microled.Nfe.LocalAgent.Unlock"u8.ToArray();

    private readonly ICertificateProvider _certificateProvider;
    private readonly ILogger<CertificateUnlockService> _logger;

    public CertificateUnlockService(
        ICertificateProvider certificateProvider,
        ILogger<CertificateUnlockService> logger)
    {
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<CertificateUnlockResponse> UnlockAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var certificate = _certificateProvider.GetCertificate();
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("O certificado selecionado nao possui chave privada.");
        }

        TryUnlockWithRsa(certificate, cancellationToken);

        _logger.LogInformation(
            "Certificate {Thumbprint} unlocked successfully for local operations",
            certificate.Thumbprint);

        return Task.FromResult(new CertificateUnlockResponse
        {
            Success = true,
            Thumbprint = certificate.Thumbprint ?? string.Empty,
            Subject = certificate.Subject ?? string.Empty,
            Message = "Certificado desbloqueado com sucesso."
        });
    }

    private void TryUnlockWithRsa(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            throw new InvalidOperationException("Nao foi possivel obter a chave privada RSA do certificado selecionado.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Force a real private-key operation so the token middleware can prompt for PIN if needed.
        _ = rsa.SignData(ProbePayload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
