using Microled.Nfe.Service.Application.DTOs.NotasFiscais;

namespace Microled.Nfe.Service.Application.Interfaces.NotasFiscais;

public interface ICreateNotaFiscalUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(CreateNotaFiscalRequest request, CancellationToken cancellationToken);
}

public interface IUpdateNotaFiscalAuthorizationUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(UpdateNotaFiscalAuthorizationRequest request, CancellationToken cancellationToken);
}

public interface IUpdateNotaFiscalStatusUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(UpdateNotaFiscalStatusRequest request, CancellationToken cancellationToken);
}

public interface IAttachNotaFiscalPdfUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(AttachNotaFiscalPdfRequest request, CancellationToken cancellationToken);
}

public interface IGetNotaFiscalByIdUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken);
}

public interface ISearchNotasFiscaisUseCase
{
    Task<ApiResponse<PagedNotaFiscalResponse>> ExecuteAsync(SearchNotasFiscaisRequest request, CancellationToken cancellationToken);
}

public interface IGetNotaFiscalXmlUseCase
{
    Task<ApiResponse<NotaFiscalXmlResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IGetNotaFiscalPdfUseCase
{
    Task<(bool Found, byte[]? Pdf)> ExecuteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IUpdateNotaFiscalPagamentoUseCase
{
    Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(UpdateNotaFiscalPagamentoRequest request, CancellationToken cancellationToken);
}
