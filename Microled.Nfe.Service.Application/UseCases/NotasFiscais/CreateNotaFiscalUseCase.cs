using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class CreateNotaFiscalUseCase : ICreateNotaFiscalUseCase
{
    private readonly INotaFiscalRepository _repository;

    public CreateNotaFiscalUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<NotaFiscalResponse>> ExecuteAsync(
        CreateNotaFiscalRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CriadoPor))
        {
            return ApiResponse<NotaFiscalResponse>.Fail("CriadoPor is required.");
        }

        var nota = NotaFiscal.Create(
            criadoPor: request.CriadoPor,
            protocolo: request.Protocolo,
            numeroRps: request.NumeroRps,
            serieRps: request.SerieRps,
            inscricaoPrestador: request.InscricaoPrestador,
            cnpjPrestador: request.CnpjPrestador,
            cpfCnpjTomador: request.CpfCnpjTomador,
            xml: request.Xml);

        await _repository.AddAsync(nota, cancellationToken);
        return ApiResponse<NotaFiscalResponse>.Ok(NotaFiscalMapper.ToResponse(nota));
    }
}
