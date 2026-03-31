using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Prepares a signed RPS batch from the transport DTOs.
/// </summary>
public interface IRpsBatchPreparationService
{
    RpsBatch PrepareSignedBatch(SendRpsRequestDto request);
}
