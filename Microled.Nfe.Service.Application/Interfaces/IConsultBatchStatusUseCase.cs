using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Use case for consulting async batch status by protocol number.
/// </summary>
public interface IConsultBatchStatusUseCase
{
    Task<ConsultBatchStatusResponseDto> ExecuteAsync(ConsultBatchStatusRequestDto request, CancellationToken cancellationToken);
}
