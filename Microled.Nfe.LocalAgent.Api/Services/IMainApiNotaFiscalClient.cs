using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.LocalAgent.Api.Services;

public interface IMainApiNotaFiscalClient
{
    Task<ApiResponse<PersistNotaFiscalBatchResponse>> PersistSendResultAsync(
        PersistRpsSendResultRequest request,
        CancellationToken cancellationToken);

    Task<ApiResponse<PersistNotaFiscalBatchResponse>> PersistBatchStatusAsync(
        PersistBatchStatusDataRequest request,
        CancellationToken cancellationToken);

    Task<ApiResponse<PersistNotaFiscalBatchResponse>> PersistConsultResultAsync(
        PersistConsultNfeResultRequest request,
        CancellationToken cancellationToken);

    Task<ApiResponse<NotaFiscalResponse>> PersistCancelResultAsync(
        PersistCancelNfeResultRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotaFiscalResponse>> SearchByProtocoloAsync(
        string protocolo,
        CancellationToken cancellationToken);
}
