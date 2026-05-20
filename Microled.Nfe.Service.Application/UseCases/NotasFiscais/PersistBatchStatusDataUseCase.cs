using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class PersistBatchStatusDataUseCase : IPersistBatchStatusDataUseCase
{
    private readonly INotaFiscalRepository _repository;

    public PersistBatchStatusDataUseCase(INotaFiscalRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistBatchStatusDataRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AlteradoPor))
        {
            return ApiResponse<PersistNotaFiscalBatchResponse>.Fail("AlteradoPor is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NumeroProtocolo))
        {
            return ApiResponse<PersistNotaFiscalBatchResponse>.Fail("NumeroProtocolo is required.");
        }

        var responses = new List<NotaFiscalResponse>();

        if (request.Autorizacoes.Count > 0)
        {
            foreach (var auth in request.Autorizacoes)
            {
                var item = MergeProtocolo(auth, request.NumeroProtocolo);
                await NotaFiscalPersistenceHelper.ApplyAuthorizationItemAsync(
                    _repository,
                    item,
                    request.AlteradoPor,
                    cancellationToken);

                var nota = await NotaFiscalPersistenceHelper.FindNotaAsync(_repository, auth, cancellationToken);
                if (nota is not null)
                {
                    responses.Add(NotaFiscalMapper.ToResponse(nota));
                }
            }
        }
        else
        {
            var notas = await _repository.ListByProtocoloAsync(request.NumeroProtocolo, cancellationToken);
            var numeroLote = request.NumeroLote?.ToString();
            var status = ResolveStatus(request);

            foreach (var nota in notas)
            {
                if (status == NotaFiscalStatus.Rejected)
                {
                    nota.SetRejected(request.AlteradoPor);
                }
                else if (status == NotaFiscalStatus.Processing)
                {
                    nota.SetStatus(NotaFiscalStatus.Processing, request.AlteradoPor);
                }
                else if (status == NotaFiscalStatus.Error)
                {
                    nota.SetError(request.AlteradoPor);
                }

                await _repository.UpdateAsync(nota, cancellationToken);
                responses.Add(NotaFiscalMapper.ToResponse(nota));
            }
        }

        return ApiResponse<PersistNotaFiscalBatchResponse>.Ok(new PersistNotaFiscalBatchResponse
        {
            ProcessedCount = responses.Count,
            Notas = responses
        });
    }

    private static NotaFiscalStatus ResolveStatus(PersistBatchStatusDataRequest request)
    {
        if (!request.Sucesso || LoteSituacaoAsync.IsInvalid(request.SituacaoCodigo))
        {
            return NotaFiscalStatus.Rejected;
        }

        if (LoteSituacaoAsync.IsPending(request.SituacaoCodigo))
        {
            return NotaFiscalStatus.Processing;
        }

        if (LoteSituacaoAsync.IsProcessed(request.SituacaoCodigo))
        {
            return NotaFiscalStatus.Authorized;
        }

        return NotaFiscalStatus.Error;
    }

    private static PersistNfeAuthorizationItemRequest MergeProtocolo(
        PersistNfeAuthorizationItemRequest auth,
        string protocolo)
    {
        if (!string.IsNullOrWhiteSpace(auth.Protocolo))
        {
            return auth;
        }

        return new PersistNfeAuthorizationItemRequest
        {
            NotaId = auth.NotaId,
            Protocolo = protocolo,
            InscricaoPrestador = auth.InscricaoPrestador,
            SerieRps = auth.SerieRps,
            NumeroRps = auth.NumeroRps,
            NumeroNota = auth.NumeroNota,
            CodigoVerificacao = auth.CodigoVerificacao,
            NumeroLote = auth.NumeroLote,
            DataEmissao = auth.DataEmissao,
            Xml = auth.Xml,
            Status = auth.Status
        };
    }
}
