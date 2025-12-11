using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Use case for canceling NFe
/// </summary>
public interface ICancelNfeUseCase
{
    Task<CancelNfeResponseDto> ExecuteAsync(CancelNfeRequestDto request, CancellationToken cancellationToken);
}

