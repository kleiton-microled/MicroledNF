using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Domain.Interfaces;

/// <summary>
/// Discovers certificates available in the machine stores.
/// </summary>
public interface ICertificateDiscoveryService
{
    Task<IReadOnlyList<CertificateDiscoveryItem>> GetAvailableCertificatesAsync(CancellationToken cancellationToken);

    Task<CertificateDiscoveryItem?> FindByThumbprintAsync(
        string thumbprint,
        string storeLocation,
        string storeName,
        CancellationToken cancellationToken);
}
