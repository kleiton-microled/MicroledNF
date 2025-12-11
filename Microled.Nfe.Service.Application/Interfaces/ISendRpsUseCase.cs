using Microled.Nfe.Service.Application.DTOs;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Use case for sending RPS batch
/// </summary>
public interface ISendRpsUseCase
{
    Task<SendRpsResponseDto> ExecuteAsync(SendRpsRequestDto request, CancellationToken cancellationToken);
}

