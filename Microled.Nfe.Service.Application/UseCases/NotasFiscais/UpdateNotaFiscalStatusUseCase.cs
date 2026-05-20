using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class UpdateNotaFiscalStatusUseCase : IUpdateNotaFiscalStatusUseCase
{
    private readonly INotaFiscalRepository _repository;

    public UpdateNotaFiscalStatusUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(
        UpdateNotaFiscalStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AlteradoPor))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("AlteradoPor is required.");
        }

        var nota = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (nota is null)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("Nota fiscal not found.");
        }

        switch (request.Status)
        {
            case NotaFiscalStatus.Cancelled:
                nota.SetCancelled(request.AlteradoPor);
                break;
            case NotaFiscalStatus.Rejected:
                nota.SetRejected(request.AlteradoPor);
                break;
            case NotaFiscalStatus.Error:
                nota.SetError(request.AlteradoPor);
                break;
            default:
                nota.SetStatus(request.Status, request.AlteradoPor);
                break;
        }

        await _repository.UpdateAsync(nota, cancellationToken);
        return ApiResponse<NotaFiscalResponse>.Ok(NotaFiscalMapper.ToResponse(nota));
    }
}
