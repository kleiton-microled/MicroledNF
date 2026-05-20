using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Resolves async batch protocol by consulting the city hall and persisting authorized notes.
/// </summary>
public interface IAsyncRpsProtocolPersistenceOrchestrator
{
    /// <summary>
    /// Polls batch status after async send and persists authorization when processed.
    /// </summary>
    Task ResolveAndPersistAsync(
        string protocolo,
        string cnpjRemetente,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists authorization from an already fetched batch status (manual protocol consult).
    /// </summary>
    Task PersistFromBatchStatusResultAsync(
        string protocolo,
        ConsultaSituacaoLoteResult statusResult,
        string cnpjRemetente,
        string actor,
        CancellationToken cancellationToken);
}
