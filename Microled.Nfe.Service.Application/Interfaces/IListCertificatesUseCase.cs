using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Lists certificates available in the local machine stores.
/// </summary>
public interface IListCertificatesUseCase
{
    Task<IReadOnlyList<CertificateListItemDto>> ExecuteAsync(CancellationToken cancellationToken);
}
