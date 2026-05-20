using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Application.Interfaces;

/// <summary>
/// Persists fiscal note lifecycle during existing emission, consult and cancel flows.
/// </summary>
public interface INotaFiscalFlowPersistenceService
{
    Task PersistRpsBatchBeforeSendAsync(DomainEntities.RpsBatch batch, string actor, CancellationToken cancellationToken);

    Task PersistRpsBatchAfterSendAsync(
        DomainEntities.RpsBatch batch,
        RetornoEnvioLoteRpsResult result,
        string actor,
        CancellationToken cancellationToken);

    Task PersistConsultResultAsync(ConsultaNfeResult result, string actor, CancellationToken cancellationToken);

    Task PersistCancelResultAsync(NfeKey chaveNFe, CancelNfeResult result, string? cancelXml, string actor, CancellationToken cancellationToken);

    Task PersistBatchStatusResultAsync(string protocolo, ConsultaSituacaoLoteResult result, string actor, CancellationToken cancellationToken);
}
