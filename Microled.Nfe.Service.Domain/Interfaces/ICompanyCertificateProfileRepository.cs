using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Domain.Interfaces;

/// <summary>
/// Persists the certificate profile selected by the user.
/// </summary>
public interface ICompanyCertificateProfileRepository
{
    Task<IReadOnlyList<CompanyCertificateProfile>> GetAllAsync(CancellationToken cancellationToken);

    Task<CompanyCertificateProfile?> GetActiveAsync(CancellationToken cancellationToken);

    Task<CompanyCertificateProfile?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken);

    Task SaveAsync(CompanyCertificateProfile profile, CancellationToken cancellationToken);

    Task SetActiveAsync(string thumbprint, CancellationToken cancellationToken);
}
