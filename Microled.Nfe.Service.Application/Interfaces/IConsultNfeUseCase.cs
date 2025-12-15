using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Use case for consulting NFe
/// </summary>
public interface IConsultNfeUseCase
{
    Task<ConsultNfeResponseDto> ExecuteAsync(ConsultNfeRequestDto request, CancellationToken cancellationToken);
}

