using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case that returns the currently active certificate profile.
/// </summary>
public class GetActiveCertificateProfileUseCase : IGetActiveCertificateProfileUseCase
{
    private readonly ICompanyCertificateProfileRepository _profileRepository;
    private readonly ICertificateDiscoveryService _certificateDiscoveryService;

    public GetActiveCertificateProfileUseCase(
        ICompanyCertificateProfileRepository profileRepository,
        ICertificateDiscoveryService certificateDiscoveryService)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _certificateDiscoveryService = certificateDiscoveryService ?? throw new ArgumentNullException(nameof(certificateDiscoveryService));
    }

    public async Task<CompanyCertificateProfileResponseDto?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetActiveAsync(cancellationToken);
        if (profile == null)
        {
            return null;
        }

        var discoveredCertificate = await _certificateDiscoveryService.FindByThumbprintAsync(
            profile.Thumbprint,
            profile.StoreLocation,
            profile.StoreName,
            cancellationToken);

        if (discoveredCertificate != null && !string.Equals(profile.Subject, discoveredCertificate.Subject, StringComparison.Ordinal))
        {
            profile.Subject = discoveredCertificate.Subject;
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await _profileRepository.SaveAsync(profile, cancellationToken);
        }

        return CompanyCertificateProfileMapper.ToResponseDto(profile);
    }
}
