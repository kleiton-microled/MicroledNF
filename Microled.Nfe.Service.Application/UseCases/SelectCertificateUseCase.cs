using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Application.UseCases;

/// <summary>
/// Use case that validates and stores the active certificate selection.
/// </summary>
public class SelectCertificateUseCase : ISelectCertificateUseCase
{
    private readonly ICertificateDiscoveryService _certificateDiscoveryService;
    private readonly ICompanyCertificateProfileRepository _profileRepository;
    private readonly ILogger<SelectCertificateUseCase> _logger;

    public SelectCertificateUseCase(
        ICertificateDiscoveryService certificateDiscoveryService,
        ICompanyCertificateProfileRepository profileRepository,
        ILogger<SelectCertificateUseCase> logger)
    {
        _certificateDiscoveryService = certificateDiscoveryService ?? throw new ArgumentNullException(nameof(certificateDiscoveryService));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SelectCertificateResponseDto> ExecuteAsync(
        SelectCertificateRequestDto request,
        CancellationToken cancellationToken)
    {
        var certificate = await _certificateDiscoveryService.FindByThumbprintAsync(
            request.Thumbprint,
            request.StoreLocation,
            request.StoreName,
            cancellationToken);

        if (certificate == null)
        {
            throw new InvalidOperationException("O certificado informado nao foi encontrado no store configurado.");
        }

        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("O certificado selecionado nao possui chave privada.");
        }

        var existingProfile = await _profileRepository.GetByThumbprintAsync(request.Thumbprint, cancellationToken);
        var profile = existingProfile ?? new CompanyCertificateProfile();

        profile.Thumbprint = certificate.Thumbprint;
        profile.Subject = certificate.Subject;
        profile.StoreLocation = certificate.StoreLocation;
        profile.StoreName = certificate.StoreName;
        profile.CompanyName = existingProfile?.CompanyName ?? string.Empty;
        profile.Cnpj = existingProfile?.Cnpj ?? certificate.Cnpj ?? string.Empty;
        profile.MunicipalRegistration = existingProfile?.MunicipalRegistration ?? string.Empty;
        profile.DefaultRemetenteCnpj = existingProfile?.DefaultRemetenteCnpj ?? certificate.Cnpj;
        profile.Environment = existingProfile?.Environment;
        profile.Notes = existingProfile?.Notes;
        profile.IsActive = existingProfile?.IsActive ?? false;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        _logger.LogInformation(
            "Selecting certificate {Thumbprint} from {StoreLocation}/{StoreName}",
            profile.Thumbprint,
            profile.StoreLocation,
            profile.StoreName);

        await _profileRepository.SaveAsync(profile, cancellationToken);
        await _profileRepository.SetActiveAsync(profile.Thumbprint, cancellationToken);

        _logger.LogInformation("Certificate {Thumbprint} selected successfully", profile.Thumbprint);

        return new SelectCertificateResponseDto
        {
            Success = true,
            Message = "Certificado selecionado com sucesso.",
            Thumbprint = profile.Thumbprint,
            Subject = profile.Subject
        };
    }
}
