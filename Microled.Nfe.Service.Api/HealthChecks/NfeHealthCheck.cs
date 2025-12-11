using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Configuration;

namespace Microled.Nfe.Service.Api.HealthChecks;

/// <summary>
/// Health check for NFS-e service configuration and certificate
/// </summary>
public class NfeHealthCheck : IHealthCheck
{
    private readonly ICertificateProvider _certificateProvider;
    private readonly NfeServiceOptions _options;
    private readonly ILogger<NfeHealthCheck> _logger;

    public NfeHealthCheck(
        ICertificateProvider certificateProvider,
        Microsoft.Extensions.Options.IOptions<NfeServiceOptions> options,
        ILogger<NfeHealthCheck> logger)
    {
        _certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // Check endpoint configuration
            var endpoint = GetEndpoint();
            if (string.IsNullOrEmpty(endpoint))
            {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "NFe service endpoint is not configured",
                data: new Dictionary<string, object> { { "Error", "Endpoint not configured" } }));
            }
            data["Endpoint"] = endpoint;

            // Check CNPJ configuration
            if (string.IsNullOrEmpty(_options.DefaultCnpjRemetente))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "DefaultCnpjRemetente is not configured",
                    data: new Dictionary<string, object> { { "Error", "CNPJ not configured" } }));
            }
            data["CnpjRemetente"] = MaskCnpj(_options.DefaultCnpjRemetente);

            // Check IM configuration
            if (string.IsNullOrEmpty(_options.DefaultIssuerIm))
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "DefaultIssuerIm is not configured",
                    data: new Dictionary<string, object> { { "Warning", "IM not configured" } }));
            }
            data["InscricaoMunicipal"] = _options.DefaultIssuerIm;

            // Check certificate
            try
            {
                var certificate = _certificateProvider.GetCertificate();
                data["CertificateSubject"] = certificate.Subject;
                data["CertificateThumbprint"] = certificate.Thumbprint;
                data["CertificateHasPrivateKey"] = certificate.HasPrivateKey;
                data["CertificateNotAfter"] = certificate.NotAfter;

                if (!certificate.HasPrivateKey)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Certificate does not have a private key",
                        data: data));
                }

                if (certificate.NotAfter < DateTime.UtcNow)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Certificate has expired",
                        data: data));
                }

                if (certificate.NotAfter < DateTime.UtcNow.AddDays(30))
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "Certificate will expire within 30 days",
                        data: data));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate for health check");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Certificate could not be loaded: {ex.Message}",
                    ex,
                    data: data));
            }

            data["Environment"] = _options.Environment ?? "Unknown";
            data["SchemaVersion"] = _options.SchemaVersion ?? "Unknown";

            return Task.FromResult(HealthCheckResult.Healthy("NFS-e service is properly configured", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                ex,
                data: data));
        }
    }

    private string GetEndpoint()
    {
        if (!string.IsNullOrEmpty(_options.BaseUrl))
            return _options.BaseUrl;

        return _options.UseProduction ? _options.ProductionEndpoint : _options.TestEndpoint;
    }

    private string MaskCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length < 8)
            return "****";

        return $"{cnpj.Substring(0, 4)}****{cnpj.Substring(cnpj.Length - 4)}";
    }
}

