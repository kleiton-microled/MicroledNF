using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Selects the active certificate used by the system.
/// </summary>
public interface ISelectCertificateUseCase
{
    Task<SelectCertificateResponseDto> ExecuteAsync(SelectCertificateRequestDto request, CancellationToken cancellationToken);
}
