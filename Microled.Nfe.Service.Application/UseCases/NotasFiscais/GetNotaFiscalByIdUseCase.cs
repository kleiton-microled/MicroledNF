using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class GetNotaFiscalByIdUseCase : IGetNotaFiscalByIdUseCase
{
    private readonly INotaFiscalRepository _repository;

    public GetNotaFiscalByIdUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nota = await _repository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("Nota fiscal not found.");
        }

        return ApiResponse<NotaFiscalResponse>.Ok(NotaFiscalMapper.ToResponse(nota));
    }
}
