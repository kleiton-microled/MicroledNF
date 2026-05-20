using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.Interfaces.NotasFiscais;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microled.Nfe.Service.Application.Services;

/// <summary>
/// After async RPS send returns a protocol, polls batch status and persists notes using authorization use case.
/// </summary>
public sealed class AsyncRpsProtocolPersistenceOrchestrator : IAsyncRpsProtocolPersistenceOrchestrator
{
    private readonly INfeGateway _nfeGateway;
    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly INotaFiscalFlowPersistenceService _notaFiscalFlowPersistence;
    private readonly IUpdateNotaFiscalAuthorizationUseCase _updateAuthorizationUseCase;
    private readonly AsyncBatchPollingOptions _pollingOptions;
    private readonly ILogger<AsyncRpsProtocolPersistenceOrchestrator> _logger;

    public AsyncRpsProtocolPersistenceOrchestrator(
        INfeGateway nfeGateway,
        INotaFiscalRepository notaFiscalRepository,
        INotaFiscalFlowPersistenceService notaFiscalFlowPersistence,
        IUpdateNotaFiscalAuthorizationUseCase updateAuthorizationUseCase,
        IOptions<AsyncBatchPollingOptions> pollingOptions,
        ILogger<AsyncRpsProtocolPersistenceOrchestrator> logger)
    {
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
        _notaFiscalRepository = notaFiscalRepository ?? throw new ArgumentNullException(nameof(notaFiscalRepository));
        _notaFiscalFlowPersistence = notaFiscalFlowPersistence ?? throw new ArgumentNullException(nameof(notaFiscalFlowPersistence));
        _updateAuthorizationUseCase = updateAuthorizationUseCase ?? throw new ArgumentNullException(nameof(updateAuthorizationUseCase));
        _pollingOptions = pollingOptions?.Value ?? new AsyncBatchPollingOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ResolveAndPersistAsync(
        string protocolo,
        string cnpjRemetente,
        string actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(protocolo))
        {
            return;
        }

        if (!_pollingOptions.Enabled)
        {
            _logger.LogInformation(
                "Async batch polling disabled. Protocol {Protocolo} stored as Processing only.",
                protocolo);
            return;
        }

        var statusResult = await PollBatchStatusAsync(protocolo, cnpjRemetente, cancellationToken);
        if (statusResult is null)
        {
            _logger.LogWarning("Could not resolve batch status for protocol {Protocolo}", protocolo);
            return;
        }

        await PersistFromBatchStatusResultAsync(protocolo, statusResult, cnpjRemetente, actor, cancellationToken);
    }

    public async Task PersistFromBatchStatusResultAsync(
        string protocolo,
        ConsultaSituacaoLoteResult statusResult,
        string cnpjRemetente,
        string actor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(protocolo))
        {
            return;
        }

        if (LoteSituacaoAsync.IsInvalid(statusResult.SituacaoCodigo))
        {
            await _notaFiscalFlowPersistence.PersistBatchStatusResultAsync(
                protocolo,
                statusResult,
                actor,
                cancellationToken);
            return;
        }

        if (LoteSituacaoAsync.IsPending(statusResult.SituacaoCodigo))
        {
            await _notaFiscalFlowPersistence.PersistBatchStatusResultAsync(
                protocolo,
                statusResult,
                actor,
                cancellationToken);

            _logger.LogInformation(
                "Protocol {Protocolo} still pending (situacao {Situacao}).",
                protocolo,
                statusResult.SituacaoCodigo);
            return;
        }

        if (!LoteSituacaoAsync.IsProcessed(statusResult.SituacaoCodigo))
        {
            await _notaFiscalFlowPersistence.PersistBatchStatusResultAsync(
                protocolo,
                statusResult,
                actor,
                cancellationToken);
            return;
        }

        await PersistAuthorizedNotesForProtocolAsync(protocolo, statusResult, actor, cancellationToken);
    }

    private async Task<ConsultaSituacaoLoteResult?> PollBatchStatusAsync(
        string protocolo,
        string cnpjRemetente,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _pollingOptions.MaxAttempts);
        var delay = TimeSpan.FromSeconds(Math.Max(1, _pollingOptions.DelaySeconds));
        ConsultaSituacaoLoteResult? lastResult = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastResult = await _nfeGateway.ConsultBatchStatusAsync(protocolo, cnpjRemetente, cancellationToken);

            _logger.LogInformation(
                "Batch status poll {Attempt}/{MaxAttempts} for protocol {Protocolo}: sucesso={Sucesso}, situacao={Situacao}",
                attempt,
                maxAttempts,
                protocolo,
                lastResult.Sucesso,
                lastResult.SituacaoCodigo);

            if (!lastResult.Sucesso || LoteSituacaoAsync.IsProcessed(lastResult.SituacaoCodigo) || LoteSituacaoAsync.IsInvalid(lastResult.SituacaoCodigo))
            {
                return lastResult;
            }

            if (attempt < maxAttempts && LoteSituacaoAsync.IsPending(lastResult.SituacaoCodigo))
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        return lastResult;
    }

    private async Task PersistAuthorizedNotesForProtocolAsync(
        string protocolo,
        ConsultaSituacaoLoteResult statusResult,
        string actor,
        CancellationToken cancellationToken)
    {
        var notas = await _notaFiscalRepository.ListByProtocoloAsync(protocolo, cancellationToken);
        if (notas.Count == 0)
        {
            _logger.LogWarning("No persisted notes found for protocol {Protocolo}", protocolo);
            return;
        }

        var numeroLote = statusResult.NumeroLote?.ToString();

        foreach (var nota in notas)
        {
            if (string.IsNullOrWhiteSpace(nota.InscricaoPrestador)
                || string.IsNullOrWhiteSpace(nota.NumeroRps)
                || !long.TryParse(nota.InscricaoPrestador, out var inscricao)
                || !long.TryParse(nota.NumeroRps, out var numeroRps))
            {
                _logger.LogWarning(
                    "Skipping note {NotaId} for protocol {Protocolo}: missing RPS identifiers",
                    nota.Id,
                    protocolo);
                continue;
            }

            var rpsKey = new RpsKey(inscricao, numeroRps, nota.SerieRps);

            var consultResult = await _nfeGateway.ConsultNfeAsync(
                new ConsultNfeCriteria { ChaveRps = rpsKey },
                cancellationToken);

            if (!consultResult.Sucesso || consultResult.NFeList.Count == 0)
            {
                _logger.LogWarning(
                    "ConsultNfe by RPS failed for note {NotaId} (protocol {Protocolo})",
                    nota.Id,
                    protocolo);
                continue;
            }

            for (var i = 0; i < consultResult.NFeList.Count; i++)
            {
                var nfe = consultResult.NFeList[i];
                var xml = i < consultResult.NotaXmlList.Count ? consultResult.NotaXmlList[i] : null;

                var authorization = await _updateAuthorizationUseCase.ExecuteAsync(
                    new UpdateNotaFiscalAuthorizationRequest
                    {
                        Id = nota.Id,
                        Protocolo = protocolo,
                        NumeroNota = nfe.ChaveNFe.NumeroNFe.ToString(),
                        CodigoVerificacao = nfe.CodigoVerificacao ?? nfe.ChaveNFe.CodigoVerificacao,
                        NumeroLote = numeroLote,
                        DataEmissao = new DateTimeOffset(nfe.DataEmissao),
                        Xml = xml,
                        AlteradoPor = actor
                    },
                    cancellationToken);

                if (!authorization.Success)
                {
                    _logger.LogWarning(
                        "Failed to persist authorization for note {NotaId}: {Message}",
                        nota.Id,
                        authorization.Message);
                }
            }
        }

        _logger.LogInformation(
            "Persisted authorized notes for protocol {Protocolo} (lote {NumeroLote}, {Count} note(s))",
            protocolo,
            numeroLote ?? "N/A",
            notas.Count);
    }
}
