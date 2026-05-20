using Microled.Nfe.Service.Application.Interfaces;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>
/// No-op persistence for hosts that do not use PostgreSQL (e.g. Local Agent).
/// </summary>
public sealed class NullNotaFiscalFlowPersistenceService : INotaFiscalFlowPersistenceService
{
    public Task PersistRpsBatchBeforeSendAsync(Domain.Entities.RpsBatch batch, string actor, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PersistRpsBatchAfterSendAsync(
        Domain.Entities.RpsBatch batch,
        RetornoEnvioLoteRpsResult result,
        string actor,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PersistConsultResultAsync(ConsultaNfeResult result, string actor, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PersistCancelResultAsync(Domain.Entities.NfeKey chaveNFe, CancelNfeResult result, string? cancelXml, string actor, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PersistBatchStatusResultAsync(string protocolo, ConsultaSituacaoLoteResult result, string actor, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class NullAsyncRpsProtocolPersistenceOrchestrator : IAsyncRpsProtocolPersistenceOrchestrator
{
    public Task ResolveAndPersistAsync(
        string protocolo,
        string cnpjRemetente,
        string actor,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PersistFromBatchStatusResultAsync(
        string protocolo,
        ConsultaSituacaoLoteResult statusResult,
        string cnpjRemetente,
        string actor,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
