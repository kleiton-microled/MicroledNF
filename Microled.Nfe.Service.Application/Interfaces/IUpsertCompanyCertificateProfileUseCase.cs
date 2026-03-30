using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Creates or updates the company data associated with a certificate profile.
/// </summary>
public interface IUpsertCompanyCertificateProfileUseCase
{
    Task<CompanyCertificateProfileResponseDto> ExecuteAsync(
        UpsertCompanyCertificateProfileRequestDto request,
        CancellationToken cancellationToken);
}
