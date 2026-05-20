using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.Service.Application.Interfaces.NotasFiscais;

public interface IPersistRpsSendResultUseCase
{
    Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistRpsSendResultRequest request,
        CancellationToken cancellationToken);
}

public interface IPersistBatchStatusDataUseCase
{
    Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistBatchStatusDataRequest request,
        CancellationToken cancellationToken);
}

public interface IPersistConsultNfeResultUseCase
{
    Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistConsultNfeResultRequest request,
        CancellationToken cancellationToken);
}

public interface IPersistCancelNfeResultUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(
        PersistCancelNfeResultRequest request,
        CancellationToken cancellationToken);
}
