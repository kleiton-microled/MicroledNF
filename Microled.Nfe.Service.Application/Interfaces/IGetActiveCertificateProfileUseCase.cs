using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Returns the currently active certificate profile.
/// </summary>
public interface IGetActiveCertificateProfileUseCase
{
    Task<CompanyCertificateProfileResponseDto?> ExecuteAsync(CancellationToken cancellationToken);
}
