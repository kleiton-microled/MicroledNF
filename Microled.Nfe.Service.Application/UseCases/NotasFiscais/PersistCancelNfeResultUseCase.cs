using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class PersistCancelNfeResultUseCase : IPersistCancelNfeResultUseCase
{
    private readonly INotaFiscalRepository _repository;

    public PersistCancelNfeResultUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(
        PersistCancelNfeResultRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AlteradoPor))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("AlteradoPor is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NumeroNota))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("NumeroNota is required.");
        }

        await NotaFiscalPersistenceHelper.ApplyAuthorizationItemAsync(
            _repository,
            new PersistNfeAuthorizationItemRequest
            {
                NumeroNota = request.NumeroNota,
                Status = Domain.Enums.NotaFiscalStatus.Cancelled,
                Xml = request.Xml
            },
            request.AlteradoPor,
            cancellationToken);

        var nota = await _repository.GetByNumeroNotaAsync(request.NumeroNota, cancellationToken);
        if (nota is null)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("Nota fiscal not found.");
        }

        return ApiResponse<NotaFiscalResponse>.Ok(NotaFiscalMapper.ToResponse(nota));
    }
}
