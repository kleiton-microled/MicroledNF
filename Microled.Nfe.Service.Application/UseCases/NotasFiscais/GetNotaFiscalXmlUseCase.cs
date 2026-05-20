using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class GetNotaFiscalXmlUseCase : IGetNotaFiscalXmlUseCase
{
    private readonly INotaFiscalRepository _repository;

    public GetNotaFiscalXmlUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalXmlResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nota = await _repository.GetByIdAsync(id, cancellationToken);
        if (nota is null)
        {
            return ApiResponse<NotaFiscalXmlResponse>.Fail("Nota fiscal not found.");
        }

        if (string.IsNullOrWhiteSpace(nota.Xml))
        {
            return ApiResponse<NotaFiscalXmlResponse>.Fail("XML not available for this note.");
        }

        return ApiResponse<NotaFiscalXmlResponse>.Ok(new NotaFiscalXmlResponse
        {
            Id = nota.Id,
            Xml = nota.Xml
        });
    }
}
