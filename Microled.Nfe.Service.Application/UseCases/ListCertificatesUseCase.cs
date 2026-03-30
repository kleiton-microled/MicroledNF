using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case that lists the certificates available for selection.
/// </summary>
public class ListCertificatesUseCase : IListCertificatesUseCase
{
    private readonly ICertificateDiscoveryService _certificateDiscoveryService;
    private readonly ICompanyCertificateProfileRepository _profileRepository;

    public ListCertificatesUseCase(
        ICertificateDiscoveryService certificateDiscoveryService,
        ICompanyCertificateProfileRepository profileRepository)
    {
        _certificateDiscoveryService = certificateDiscoveryService ?? throw new ArgumentNullException(nameof(certificateDiscoveryService));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
    }

    public async Task<IReadOnlyList<CertificateListItemDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var certificates = await _certificateDiscoveryService.GetAvailableCertificatesAsync(cancellationToken);
        var activeProfile = await _profileRepository.GetActiveAsync(cancellationToken);
        var activeThumbprint = NormalizeThumbprint(activeProfile?.Thumbprint);

        return certificates
            .Select(certificate => new CertificateListItemDto
            {
                Thumbprint = certificate.Thumbprint,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                SerialNumber = certificate.SerialNumber,
                NotBefore = certificate.NotBefore,
                NotAfter = certificate.NotAfter,
                HasPrivateKey = certificate.HasPrivateKey,
                StoreLocation = certificate.StoreLocation,
                StoreName = certificate.StoreName,
                SimpleName = certificate.SimpleName,
                Cnpj = certificate.Cnpj,
                Cpf = certificate.Cpf,
                IsA3Candidate = certificate.IsA3Candidate,
                IsCurrentlySelected =
                    activeProfile != null &&
                    NormalizeThumbprint(certificate.Thumbprint) == activeThumbprint &&
                    string.Equals(certificate.StoreLocation, activeProfile.StoreLocation, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(certificate.StoreName, activeProfile.StoreName, StringComparison.OrdinalIgnoreCase)
            })
            .OrderByDescending(item => item.IsCurrentlySelected)
            .ThenByDescending(item => item.HasPrivateKey)
            .ThenByDescending(item => item.IsA3Candidate)
            .ThenBy(item => item.SimpleName ?? item.Subject, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeThumbprint(string? thumbprint)
    {
        return string.IsNullOrWhiteSpace(thumbprint)
            ? string.Empty
            : thumbprint.Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(":", string.Empty)
                .ToUpperInvariant();
    }
}
