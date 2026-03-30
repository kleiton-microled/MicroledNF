using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case that saves the company data linked to a certificate profile.
/// </summary>
public class UpsertCompanyCertificateProfileUseCase : IUpsertCompanyCertificateProfileUseCase
{
    private readonly ICompanyCertificateProfileRepository _profileRepository;
    private readonly ICertificateDiscoveryService _certificateDiscoveryService;
    private readonly ILogger<UpsertCompanyCertificateProfileUseCase> _logger;

    public UpsertCompanyCertificateProfileUseCase(
        ICompanyCertificateProfileRepository profileRepository,
        ICertificateDiscoveryService certificateDiscoveryService,
        ILogger<UpsertCompanyCertificateProfileUseCase> logger)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _certificateDiscoveryService = certificateDiscoveryService ?? throw new ArgumentNullException(nameof(certificateDiscoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CompanyCertificateProfileResponseDto> ExecuteAsync(
        UpsertCompanyCertificateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var existingProfile = await _profileRepository.GetByThumbprintAsync(request.Thumbprint, cancellationToken);
        var discoveredCertificate = existingProfile == null
            ? await FindDiscoveredCertificateAsync(request.Thumbprint, cancellationToken)
            : null;

        if (existingProfile == null && discoveredCertificate == null)
        {
            throw new InvalidOperationException("Nao foi possivel localizar um certificado para o thumbprint informado.");
        }

        var profile = existingProfile ?? new CompanyCertificateProfile
        {
            Thumbprint = discoveredCertificate!.Thumbprint,
            Subject = discoveredCertificate.Subject,
            StoreLocation = discoveredCertificate.StoreLocation,
            StoreName = discoveredCertificate.StoreName,
            DefaultRemetenteCnpj = discoveredCertificate.Cnpj
        };

        profile.Subject = string.IsNullOrWhiteSpace(profile.Subject)
            ? discoveredCertificate?.Subject ?? string.Empty
            : profile.Subject;
        profile.CompanyName = request.CompanyName.Trim();
        profile.Cnpj = DigitsOnly(request.Cnpj);
        profile.MunicipalRegistration = request.MunicipalRegistration.Trim();
        profile.DefaultRemetenteCnpj = string.IsNullOrWhiteSpace(request.DefaultRemetenteCnpj)
            ? null
            : DigitsOnly(request.DefaultRemetenteCnpj);
        profile.Environment = NormalizeOptionalValue(request.Environment);
        profile.Notes = NormalizeOptionalValue(request.Notes);
        profile.UpdatedAtUtc = DateTime.UtcNow;

        _logger.LogInformation("Upserting company profile for certificate {Thumbprint}", profile.Thumbprint);

        await _profileRepository.SaveAsync(profile, cancellationToken);

        _logger.LogInformation("Company profile saved for certificate {Thumbprint}", profile.Thumbprint);

        return CompanyCertificateProfileMapper.ToResponseDto(profile);
    }

    private async Task<CertificateDiscoveryItem?> FindDiscoveredCertificateAsync(
        string thumbprint,
        CancellationToken cancellationToken)
    {
        var certificates = await _certificateDiscoveryService.GetAvailableCertificatesAsync(cancellationToken);
        return certificates.FirstOrDefault(item =>
            string.Equals(NormalizeThumbprint(item.Thumbprint), NormalizeThumbprint(thumbprint), StringComparison.Ordinal));
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

    private static string DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
