using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class AttachNotaFiscalPdfUseCase : IAttachNotaFiscalPdfUseCase
{
    private readonly INotaFiscalRepository _repository;

    public AttachNotaFiscalPdfUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(
        AttachNotaFiscalPdfRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AlteradoPor))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("AlteradoPor is required.");
        }

        if (request.Pdf is null || request.Pdf.Length == 0)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("PDF content cannot be empty.");
        }

        var nota = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (nota is null)
        {
            return ApiResponse<NotaFiscalResponse>.Fail("Nota fiscal not found.");
        }

        nota.AttachPdf(request.Pdf, request.AlteradoPor);
        await _repository.UpdateAsync(nota, cancellationToken);
        return ApiResponse<NotaFiscalResponse>.Ok(NotaFiscalMapper.ToResponse(nota));
    }
}
