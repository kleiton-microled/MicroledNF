using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Configuration;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Certificate provider implementation
/// </summary>
public class CertificateProvider : ICertificateProvider
{
    private readonly NfeServiceOptions _options;
    private readonly ILogger<CertificateProvider> _logger;
    private X509Certificate2? _certificate;

    public CertificateProvider(IOptions<NfeServiceOptions> options, ILogger<CertificateProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public X509Certificate2 GetCertificate()
    {
        if (_certificate != null)
            return _certificate;

        var certOptions = _options.Certificate;
        var mode = (certOptions.Mode ?? "File").Trim();

        if (string.Equals(mode, "File", StringComparison.OrdinalIgnoreCase))
        {
            return LoadCertificateFromFile(certOptions);
        }
        else if (string.Equals(mode, "Store", StringComparison.OrdinalIgnoreCase))
        {
            return LoadCertificateFromStore(certOptions);
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid certificate mode '{mode}'. Must be either 'File' or 'Store'. " +
                "Please configure Certificate:Mode in appsettings.json");
        }
    }

    private X509Certificate2 LoadCertificateFromFile(CertificateOptions certOptions)
    {
        if (string.IsNullOrEmpty(certOptions.FilePath))
        {
            throw new InvalidOperationException(
                "Certificate:Mode is set to 'File' but Certificate:FilePath is not configured. " +
                "Please provide the path to the PFX certificate file in appsettings.json");
        }

        try
        {
            X509Certificate2 cert;
            if (string.IsNullOrEmpty(certOptions.Password))
            {
                cert = new X509Certificate2(certOptions.FilePath);
            }
            else
            {
                cert = new X509Certificate2(certOptions.FilePath, certOptions.Password);
            }

            // Verify certificate has private key
            if (!cert.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    $"Certificate loaded from file '{certOptions.FilePath}' does not have a private key. " +
                    "The certificate must have a private key for signing operations.");
            }

            _certificate = cert;
            _logger.LogInformation("Certificate loaded from file: {FilePath} (Subject: {Subject}, Thumbprint: {Thumbprint})",
                certOptions.FilePath, cert.Subject, cert.Thumbprint);
            return _certificate;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate from file: {FilePath}", certOptions.FilePath);
            throw new InvalidOperationException(
                $"Error loading certificate from file '{certOptions.FilePath}': {ex.Message}. " +
                "Please verify the file path and password in appsettings.json", ex);
        }
    }

    private X509Certificate2 LoadCertificateFromStore(CertificateOptions certOptions)
    {
        if (string.IsNullOrEmpty(certOptions.Thumbprint))
        {
            throw new InvalidOperationException(
                "Certificate:Mode is set to 'Store' but Certificate:Thumbprint is not configured. " +
                "Please provide the certificate thumbprint in appsettings.json");
        }

        try
        {
            // Normalize thumbprint: remove spaces, hyphens, colons and convert to uppercase
            var normalizedThumbprint = certOptions.Thumbprint
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(":", "")
                .ToUpperInvariant();

            var storeLocation = Enum.Parse<StoreLocation>(certOptions.StoreLocation ?? "CurrentUser");
            var storeName = Enum.Parse<StoreName>(certOptions.StoreName ?? "My");

            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            // Try to find by normalized thumbprint
            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                normalizedThumbprint,
                false);

            if (certificates.Count == 0)
            {
                // If not found, try with original thumbprint (in case it's already normalized)
                certificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    certOptions.Thumbprint,
                    false);
            }

            if (certificates.Count > 0)
            {
                var cert = certificates[0];

                // Verify certificate has private key
                if (!cert.HasPrivateKey)
                {
                    throw new InvalidOperationException(
                        $"Certificate with thumbprint {normalizedThumbprint} was found but does not have a private key. " +
                        "The certificate must have a private key for signing operations.");
                }

                _certificate = cert;
                _logger.LogInformation("Certificate loaded from store: {Thumbprint} (Location: {StoreLocation}, Store: {StoreName}, Subject: {Subject})",
                    normalizedThumbprint, storeLocation, storeName, cert.Subject);
                return _certificate;
            }

            throw new InvalidOperationException(
                $"Certificate with thumbprint {normalizedThumbprint} not found in store '{storeName}' at location '{storeLocation}'. " +
                "Please verify the thumbprint and store configuration in appsettings.json");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate from store: {Thumbprint}", certOptions.Thumbprint);
            throw new InvalidOperationException(
                $"Error loading certificate from store: {ex.Message}. " +
                "Please verify the certificate configuration in appsettings.json", ex);
        }
    }
}

