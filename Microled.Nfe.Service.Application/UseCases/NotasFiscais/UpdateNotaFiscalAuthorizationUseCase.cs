using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using DomainNotaFiscal = Microled.Nfe.Service.Domain.Entities.NotaFiscal;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class UpdateNotaFiscalAuthorizationUseCase : IUpdateNotaFiscalAuthorizationUseCase
{
    private readonly INotaFiscalRepository _repository;

    public UpdateNotaFiscalAuthorizationUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(
        UpdateNotaFiscalAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AlteradoPor))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("AlteradoPor is required.");
        }

        if (!request.Id.HasValue && string.IsNullOrWhiteSpace(request.Protocolo))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("Id or Protocolo is required.");
        }

        DomainNotaFiscal? nota;
        if (request.Id.HasValue)
        {
            nota = await _repository.GetByIdAsync(request.Id.Value, cancellationToken);
        }
        else
        {
            nota = await _repository.GetByProtocoloAsync(request.Protocolo!, cancellationToken);
        }

        if (nota is null)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("Nota fiscal not found.");
        }

        if (string.IsNullOrWhiteSpace(request.NumeroNota)
            && nota.Status != NotaFiscalStatus.Processing)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("NumeroNota is required unless the note is still processing.");
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroNota))
        {
            nota.SetAuthorized(
                request.NumeroNota,
                request.AlteradoPor,
                request.CodigoVerificacao,
                request.NumeroLote,
                request.DataEmissao,
                request.Xml);
        }
        else if (!string.IsNullOrWhiteSpace(request.Xml))
        {
            nota.UpdateXml(request.Xml, request.AlteradoPor);
        }

        await _repository.UpdateAsync(nota, cancellationToken);
        return ApiResponse<NotaFiscalResponse>.Ok(NotaFiscalMapper.ToResponse(nota));
    }
}
