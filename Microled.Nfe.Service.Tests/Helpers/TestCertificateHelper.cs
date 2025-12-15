using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microled.Nfe.Service.Tests.Helpers;

/// <summary>
/// Helper class for creating test certificates
/// </summary>
public static class TestCertificateHelper
{
    /// <summary>
    /// Creates a self-signed certificate with private key for testing
    /// </summary>
    public static X509Certificate2 CreateTestCertificateWithPrivateKey()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test Certificate", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));

        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
        
        // Export and re-import to ensure we have the private key
        var pfxBytes = certificate.Export(X509ContentType.Pfx, "test");
        return new X509Certificate2(pfxBytes, "test", X509KeyStorageFlags.Exportable);
    }

    /// <summary>
    /// Creates a certificate without private key for testing
    /// </summary>
    public static X509Certificate2 CreateTestCertificateWithoutPrivateKey()
    {
        var certWithKey = CreateTestCertificateWithPrivateKey();
        // Export only the public key
        var publicKeyBytes = certWithKey.Export(X509ContentType.Cert);
        return new X509Certificate2(publicKeyBytes);
    }
}

