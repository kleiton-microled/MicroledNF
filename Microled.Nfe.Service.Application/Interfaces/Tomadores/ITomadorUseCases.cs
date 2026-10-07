using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.DTOs.Tomadores;

namespace Microled.Nfe.Service.Application.Interfaces.Tomadores;

public interface ISearchTomadoresUseCase
{
    Task<ApiResponse<PagedTomadorResponse>> ExecuteAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IGetTomadorByCpfCnpjUseCase
{
    Task<ApiResponse<TomadorResponse>> ExecuteAsync(string cpfCnpj, CancellationToken cancellationToken);
}

public interface ICreateTomadorUseCase
{
    Task<ApiResponse<TomadorResponse>> ExecuteAsync(TomadorRequest request, CancellationToken cancellationToken);
}

public interface IUpdateTomadorUseCase
{
    Task<ApiResponse<TomadorResponse>> ExecuteAsync(Guid id, TomadorRequest request, CancellationToken cancellationToken);
}

public interface IDeleteTomadorUseCase
{
    Task<ApiResponse<bool>> ExecuteAsync(Guid id, CancellationToken cancellationToken);
}
