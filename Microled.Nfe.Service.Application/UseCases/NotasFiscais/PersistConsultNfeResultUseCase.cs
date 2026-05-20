using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class PersistConsultNfeResultUseCase : IPersistConsultNfeResultUseCase
{
    private readonly INotaFiscalRepository _repository;

    public PersistConsultNfeResultUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistConsultNfeResultRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AlteradoPor))
        {
            return ApiResponse<PersistNotaFiscalBatchResponse>.Fail("AlteradoPor is required.");
        }

        var responses = new List<NotaFiscalResponse>();
        foreach (var auth in request.Autorizacoes)
        {
            await NotaFiscalPersistenceHelper.ApplyAuthorizationItemAsync(
                _repository,
                auth,
                request.AlteradoPor,
                cancellationToken);

            var nota = await NotaFiscalPersistenceHelper.FindNotaAsync(_repository, auth, cancellationToken);
            if (nota is not null)
            {
                responses.Add(NotaFiscalMapper.ToResponse(nota));
            }
        }

        return ApiResponse<PersistNotaFiscalBatchResponse>.Ok(new PersistNotaFiscalBatchResponse
        {
            ProcessedCount = responses.Count,
            Notas = responses
        });
    }
}
