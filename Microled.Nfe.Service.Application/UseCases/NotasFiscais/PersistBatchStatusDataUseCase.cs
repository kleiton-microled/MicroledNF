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
        var autorizacoes = request.Autorizacoes.Count > 0
            ? request.Autorizacoes
            : BuildAutorizacoesFromResultadoOperacao(request);

        if (autorizacoes.Count > 0)
        {
            foreach (var auth in autorizacoes)
            {
                var item = MergeProtocolo(auth, request.NumeroProtocolo);
                await NotaFiscalPersistenceHelper.ApplyAuthorizationItemAsync(
                    _repository,
                    item,
                    request.AlteradoPor,
                    cancellationToken);

                var nota = await NotaFiscalPersistenceHelper.FindNotaAsync(_repository, item, cancellationToken);
                if (nota is not null)
                {
                    responses.Add(NotaFiscalMapper.ToResponse(nota));
                }
            }
        }

        var notasByProtocolo = await _repository.ListByProtocoloAsync(request.NumeroProtocolo, cancellationToken);
        if (notasByProtocolo.Count > 0)
        {
            var status = ResolveStatus(request);
            var resultadoXml = request.ResultadoOperacao;

            foreach (var nota in notasByProtocolo)
            {
                if (responses.Any(r => r.Id == nota.Id))
                {
                    continue;
                }

                if (status == NotaFiscalStatus.Rejected)
                {
                    nota.SetRejected(request.AlteradoPor, resultadoXml);
                }
                else if (status == NotaFiscalStatus.Processing)
                {
                    nota.SetStatus(NotaFiscalStatus.Processing, request.AlteradoPor);
                }
                else if (status == NotaFiscalStatus.Error)
                {
                    nota.SetError(request.AlteradoPor, resultadoXml);
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

    private static List<PersistNfeAuthorizationItemRequest> BuildAutorizacoesFromResultadoOperacao(
        PersistBatchStatusDataRequest request)
    {
        var events = RetornoEnvioLoteRpsResultadoParser.ParseRpsEvents(request.ResultadoOperacao);
        if (events.Count == 0)
        {
            return [];
        }

        var status = ResolveStatus(request);

        return events
            .Where(e => e.IsErro)
            .Select(e => new PersistNfeAuthorizationItemRequest
            {
                Protocolo = request.NumeroProtocolo,
                InscricaoPrestador = e.InscricaoPrestador,
                SerieRps = e.SerieRps,
                NumeroRps = e.NumeroRps,
                NumeroLote = request.NumeroLote?.ToString(),
                Xml = request.ResultadoOperacao,
                Status = e.IsErro ? NotaFiscalStatus.Rejected : status
            })
            .ToList();
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
