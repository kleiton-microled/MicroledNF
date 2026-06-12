using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Microled.Nfe.Service.Application.UseCases.NotasFiscais;

public sealed class PersistBatchStatusDataUseCase : IPersistBatchStatusDataUseCase
{
    private readonly INotaFiscalRepository _repository;
    private readonly ILogger<PersistBatchStatusDataUseCase> _logger;

    public PersistBatchStatusDataUseCase(
        INotaFiscalRepository repository,
        ILogger<PersistBatchStatusDataUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<PersistNotaFiscalBatchResponse>> ExecuteAsync(
        PersistBatchStatusDataRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PersistBatchStatus START: Protocolo={Protocolo} Sucesso={Sucesso} Situacao={Situacao} Autorizacoes={Autorizacoes} HasResultado={HasResultado}",
            request.NumeroProtocolo,
            request.Sucesso,
            request.SituacaoCodigo,
            request.Autorizacoes.Count,
            !string.IsNullOrWhiteSpace(request.ResultadoOperacao));

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

        _logger.LogInformation(
            "PersistBatchStatus: Protocolo={Protocolo} AutorizacoesParaProcessar={Count}",
            request.NumeroProtocolo,
            autorizacoes.Count);

        if (autorizacoes.Count > 0)
        {
            for (var i = 0; i < autorizacoes.Count; i++)
            {
                var auth = autorizacoes[i];
                var item = MergeProtocolo(auth, request.NumeroProtocolo);

                _logger.LogInformation(
                    "PersistBatchStatus: Processando autorizacao [{Index}/{Total}] NotaId={NotaId} NumeroNota={NumeroNota} Status={Status} NumeroRps={NumeroRps}",
                    i + 1,
                    autorizacoes.Count,
                    item.NotaId,
                    item.NumeroNota ?? "(null)",
                    item.Status,
                    item.NumeroRps ?? "(null)");

                try
                {
                    await NotaFiscalPersistenceHelper.ApplyAuthorizationItemAsync(
                        _repository,
                        item,
                        request.AlteradoPor,
                        cancellationToken,
                        _logger);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "PersistBatchStatus FALHOU em ApplyAuthorizationItemAsync [{Index}/{Total}]: NotaId={NotaId} NumeroNota={NumeroNota} Status={Status}",
                        i + 1,
                        autorizacoes.Count,
                        item.NotaId,
                        item.NumeroNota ?? "(null)",
                        item.Status);
                    throw;
                }

                var nota = await NotaFiscalPersistenceHelper.FindNotaAsync(_repository, item, cancellationToken);
                if (nota is not null)
                {
                    responses.Add(NotaFiscalMapper.ToResponse(nota));
                    _logger.LogInformation(
                        "PersistBatchStatus: Autorizacao [{Index}] persistida — NotaId={NotaId} StatusFinal={Status}",
                        i + 1,
                        nota.Id,
                        nota.Status);
                }
                else
                {
                    _logger.LogWarning(
                        "PersistBatchStatus: Autorizacao [{Index}] aplicada mas nota nao encontrada apos persistencia. NotaId={NotaId} NumeroNota={NumeroNota}",
                        i + 1,
                        item.NotaId,
                        item.NumeroNota ?? "(null)");
                }
            }
        }

        _logger.LogInformation(
            "PersistBatchStatus: Buscando notas por protocolo={Protocolo}",
            request.NumeroProtocolo);

        var notasByProtocolo = await _repository.ListByProtocoloAsync(request.NumeroProtocolo, cancellationToken);

        _logger.LogInformation(
            "PersistBatchStatus: Encontradas {Count} notas pelo protocolo={Protocolo}",
            notasByProtocolo.Count,
            request.NumeroProtocolo);

        if (notasByProtocolo.Count > 0)
        {
            var status = ResolveStatus(request);
            var resultadoXml = request.ResultadoOperacao;

            foreach (var nota in notasByProtocolo)
            {
                if (responses.Any(r => r.Id == nota.Id))
                {
                    _logger.LogInformation(
                        "PersistBatchStatus: Nota {NotaId} ja processada via autorizacoes, pulando.",
                        nota.Id);
                    continue;
                }

                _logger.LogInformation(
                    "PersistBatchStatus: Aplicando status={Status} na nota {NotaId} via protocolo",
                    status,
                    nota.Id);

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

                try
                {
                    await _repository.UpdateAsync(nota, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "PersistBatchStatus FALHOU em UpdateAsync para nota {NotaId} status={Status}",
                        nota.Id,
                        status);
                    throw;
                }

                responses.Add(NotaFiscalMapper.ToResponse(nota));
            }
        }

        _logger.LogInformation(
            "PersistBatchStatus OK: Protocolo={Protocolo} ProcessedCount={Count}",
            request.NumeroProtocolo,
            responses.Count);

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
