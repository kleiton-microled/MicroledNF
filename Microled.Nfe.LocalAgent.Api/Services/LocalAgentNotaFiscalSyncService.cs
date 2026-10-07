using Microsoft.Extensions.Options;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.Service.Application.Configuration;
using Microled.Nfe.Service.Application.Services;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Infra.Repositories;

namespace Microled.Nfe.LocalAgent.Api.Services;

/// <summary>
/// Forwards SOAP results from LocalAgent to the main API for PostgreSQL persistence.
/// </summary>
public sealed class LocalAgentNotaFiscalSyncService
{
    private const string Actor = "localagent";

    private readonly IMainApiNotaFiscalClient _mainApiClient;
    private readonly INfeGateway _nfeGateway;
    private readonly IAccessRpsRepository _accessRpsRepository;
    private readonly NfeIntegrationOptions _integrationOptions;
    private readonly ILogger<LocalAgentNotaFiscalSyncService> _logger;

    public LocalAgentNotaFiscalSyncService(
        IMainApiNotaFiscalClient mainApiClient,
        INfeGateway nfeGateway,
        IAccessRpsRepository accessRpsRepository,
        IOptions<NfeIntegrationOptions> integrationOptions,
        ILogger<LocalAgentNotaFiscalSyncService> logger)
    {
        _mainApiClient = mainApiClient ?? throw new ArgumentNullException(nameof(mainApiClient));
        _nfeGateway = nfeGateway ?? throw new ArgumentNullException(nameof(nfeGateway));
        _accessRpsRepository = accessRpsRepository ?? throw new ArgumentNullException(nameof(accessRpsRepository));
        _integrationOptions = integrationOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SyncSendResultAsync(
        SendRpsRequestDto request,
        SendRpsResponseDto response,
        CancellationToken cancellationToken,
        Guid? notaId = null)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning(
                "Main API persist/send-result skipped: NfeIntegration:MainApiBaseUrl is not configured.");
            return;
        }

        var persistRequest = new PersistRpsSendResultRequest
        {
            CriadoPor = Actor,
            Protocolo = response.Protocolo,
            Sucesso = response.Sucesso,
            CnpjPrestador = request.Prestador.CpfCnpj,
            Itens = request.RpsList.Select(rps => new PersistRpsItemRequest
            {
                NotaId = request.RpsList.Count == 1 ? notaId : null,
                NumeroRps = rps.NumeroRps.ToString(),
                SerieRps = rps.SerieRps,
                InscricaoPrestador = rps.InscricaoPrestador.ToString(),
                CpfCnpjTomador = rps.Tomador?.CpfCnpj
            }).ToList(),
            Autorizacoes = MapAutorizacoes(response.ChavesNFeRPS, response.Protocolo),
            Erros = response.Erros
                .Select(e => new NotaFiscalEventoDto
                {
                    Codigo = e.Codigo.ToString(),
                    Descricao = e.Descricao ?? string.Empty
                })
                .ToList()
        };

        _logger.LogInformation(
            "Forwarding SOAP send-result to Main API for DB persist: Protocolo={Protocolo} Sucesso={Sucesso} Itens={Itens} Autorizacoes={Autorizacoes} BaseUrl={BaseUrl}",
            persistRequest.Protocolo ?? "(null)",
            persistRequest.Sucesso,
            persistRequest.Itens.Count,
            persistRequest.Autorizacoes.Count,
            _integrationOptions.MainApiBaseUrl);

        await PersistAndLogAsync(
            "send-result",
            () => _mainApiClient.PersistSendResultAsync(persistRequest, cancellationToken));

        await RecordAuthorizedNotasInAccessAsync(persistRequest.Autorizacoes, cancellationToken);
    }

    public async Task SyncBatchStatusAsync(
        ConsultBatchStatusRequestDto request,
        ConsultaSituacaoLoteResult gatewayResult,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning(
                "Main API persist/batch-status skipped: NfeIntegration:MainApiBaseUrl is not configured.");
            return;
        }

        var autorizacoes = BuildAutorizacoesFromResultado(gatewayResult, request.NumeroProtocolo);

        if (autorizacoes.Count == 0
            && gatewayResult.Sucesso
            && LoteSituacaoAsync.IsProcessed(gatewayResult.SituacaoCodigo))
        {
            autorizacoes = await ResolveAuthorizationsForProtocolAsync(
                request.NumeroProtocolo,
                gatewayResult.NumeroLote?.ToString(),
                cancellationToken);
        }

        var persistRequest = new PersistBatchStatusDataRequest
        {
            NumeroProtocolo = request.NumeroProtocolo,
            Sucesso = gatewayResult.Sucesso,
            SituacaoCodigo = gatewayResult.SituacaoCodigo,
            NumeroLote = gatewayResult.NumeroLote,
            DataProcessamento = gatewayResult.DataProcessamento.HasValue
                ? new DateTimeOffset(gatewayResult.DataProcessamento.Value)
                : null,
            AlteradoPor = Actor,
            ResultadoOperacao = gatewayResult.ResultadoOperacao,
            Autorizacoes = autorizacoes
        };

        _logger.LogInformation(
            "Forwarding batch status to Main API for DB persist: Protocolo={Protocolo} Situacao={SituacaoCodigo}/{SituacaoNome} Autorizacoes={Autorizacoes} HasResultadoOperacao={HasResultado}",
            request.NumeroProtocolo,
            gatewayResult.SituacaoCodigo,
            gatewayResult.SituacaoNome,
            autorizacoes.Count,
            !string.IsNullOrWhiteSpace(gatewayResult.ResultadoOperacao));

        await PersistAndLogAsync(
            "batch-status",
            () => _mainApiClient.PersistBatchStatusAsync(persistRequest, cancellationToken));

        await RecordAuthorizedNotasInAccessAsync(autorizacoes, cancellationToken);
    }

    /// <summary>
    /// Para cada NFS-e autorizada, grava a linha na tabela NF do Access a partir do RPS de origem.
    /// Falhas aqui nao afetam a persistencia principal (apenas log).
    /// </summary>
    private async Task RecordAuthorizedNotasInAccessAsync(
        IEnumerable<PersistNfeAuthorizationItemRequest> autorizacoes,
        CancellationToken cancellationToken)
    {
        foreach (var autorizacao in autorizacoes)
        {
            if (autorizacao.Status != NotaFiscalStatus.Authorized
                || string.IsNullOrWhiteSpace(autorizacao.NumeroNota)
                || string.IsNullOrWhiteSpace(autorizacao.NumeroRps))
            {
                continue;
            }

            try
            {
                var result = await _accessRpsRepository.InsertNfFromRpsAsync(
                    new AccessNfEmitida
                    {
                        NumeroNota = autorizacao.NumeroNota,
                        NumeroRps = autorizacao.NumeroRps,
                        DataEmissao = autorizacao.DataEmissao?.LocalDateTime
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Tabela NF do Access: NFS-e {NumeroNota} (RPS {NumeroRps}) -> {Result}",
                    autorizacao.NumeroNota,
                    autorizacao.NumeroRps,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao gravar NFS-e {NumeroNota} (RPS {NumeroRps}) na tabela NF do Access.",
                    autorizacao.NumeroNota,
                    autorizacao.NumeroRps);
            }
        }
    }

    private static List<PersistNfeAuthorizationItemRequest> BuildAutorizacoesFromResultado(
        ConsultaSituacaoLoteResult gatewayResult,
        string protocolo)
    {
        var events = RetornoEnvioLoteRpsResultadoParser.ParseRpsEvents(gatewayResult.ResultadoOperacao);
        if (events.Count == 0)
        {
            return [];
        }

        var status = LoteSituacaoAsync.IsInvalid(gatewayResult.SituacaoCodigo)
            ? NotaFiscalStatus.Rejected
            : LoteSituacaoAsync.IsProcessed(gatewayResult.SituacaoCodigo)
                ? NotaFiscalStatus.Authorized
                : NotaFiscalStatus.Processing;

        return events
            .Where(e => e.IsErro)
            .Select(e => new PersistNfeAuthorizationItemRequest
            {
                Protocolo = protocolo,
                InscricaoPrestador = e.InscricaoPrestador,
                SerieRps = e.SerieRps,
                NumeroRps = e.NumeroRps,
                NumeroLote = gatewayResult.NumeroLote?.ToString(),
                Xml = gatewayResult.ResultadoOperacao,
                Status = e.IsErro ? NotaFiscalStatus.Rejected : status
            })
            .ToList();
    }

    public async Task SyncConsultResultAsync(
        ConsultNfeResponseDto response,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled() || !response.Sucesso || response.NFeList.Count == 0)
        {
            return;
        }

        var persistRequest = new PersistConsultNfeResultRequest
        {
            AlteradoPor = Actor,
            Autorizacoes = response.NFeList.Select(nfe => new PersistNfeAuthorizationItemRequest
            {
                InscricaoPrestador = nfe.ChaveNFe.InscricaoPrestador.ToString(),
                NumeroNota = nfe.ChaveNFe.NumeroNFe.ToString(),
                CodigoVerificacao = nfe.CodigoVerificacao ?? nfe.ChaveNFe.CodigoVerificacao,
                DataEmissao = new DateTimeOffset(nfe.DataEmissao),
                Xml = nfe.NotaXml,
                Status = NotaFiscalStatus.Authorized
            }).ToList()
        };

        await PersistAndLogAsync(
            "consult-result",
            () => _mainApiClient.PersistConsultResultAsync(persistRequest, cancellationToken));
    }

    public async Task SyncCancelResultAsync(
        CancelNfeRequestDto request,
        CancelNfeResponseDto response,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled() || !response.Sucesso)
        {
            return;
        }

        var persistRequest = new PersistCancelNfeResultRequest
        {
            NumeroNota = request.ChaveNFe.NumeroNFe.ToString(),
            AlteradoPor = Actor
        };

        await PersistAndLogAsync(
            "cancel-result",
            () => _mainApiClient.PersistCancelResultAsync(persistRequest, cancellationToken));
    }

    private async Task<List<PersistNfeAuthorizationItemRequest>> ResolveAuthorizationsForProtocolAsync(
        string protocolo,
        string? numeroLote,
        CancellationToken cancellationToken)
    {
        var notas = await _mainApiClient.SearchByProtocoloAsync(protocolo, cancellationToken);
        var autorizacoes = new List<PersistNfeAuthorizationItemRequest>();

        foreach (var nota in notas)
        {
            if (string.IsNullOrWhiteSpace(nota.InscricaoPrestador)
                || string.IsNullOrWhiteSpace(nota.NumeroRps)
                || !long.TryParse(nota.InscricaoPrestador, out var inscricao)
                || !long.TryParse(nota.NumeroRps, out var numeroRps))
            {
                continue;
            }

            var consultResult = await _nfeGateway.ConsultNfeAsync(
                new ConsultNfeCriteria
                {
                    ChaveRps = new RpsKey(inscricao, numeroRps, nota.SerieRps)
                },
                cancellationToken);

            if (!consultResult.Sucesso || consultResult.NFeList.Count == 0)
            {
                _logger.LogWarning(
                    "Could not resolve NF-e for protocol {Protocolo}, RPS {NumeroRps}",
                    protocolo,
                    nota.NumeroRps);
                continue;
            }

            for (var i = 0; i < consultResult.NFeList.Count; i++)
            {
                var nfe = consultResult.NFeList[i];
                var xml = i < consultResult.NotaXmlList.Count ? consultResult.NotaXmlList[i] : null;

                autorizacoes.Add(new PersistNfeAuthorizationItemRequest
                {
                    NotaId = nota.Id,
                    Protocolo = protocolo,
                    InscricaoPrestador = nota.InscricaoPrestador,
                    SerieRps = nota.SerieRps,
                    NumeroRps = nota.NumeroRps,
                    NumeroNota = nfe.ChaveNFe.NumeroNFe.ToString(),
                    CodigoVerificacao = nfe.CodigoVerificacao ?? nfe.ChaveNFe.CodigoVerificacao,
                    NumeroLote = numeroLote,
                    DataEmissao = new DateTimeOffset(nfe.DataEmissao),
                    Xml = xml,
                    Status = NotaFiscalStatus.Authorized
                });
            }
        }

        return autorizacoes;
    }

    private static List<PersistNfeAuthorizationItemRequest> MapAutorizacoes(
        IReadOnlyList<NfeRpsKeyDto> chaves,
        string? protocolo)
    {
        return chaves.Select(par => new PersistNfeAuthorizationItemRequest
        {
            Protocolo = protocolo,
            InscricaoPrestador = par.ChaveRPS.InscricaoPrestador.ToString(),
            SerieRps = par.ChaveRPS.SerieRps,
            NumeroRps = par.ChaveRPS.NumeroRps.ToString(),
            NumeroNota = par.ChaveNFe.NumeroNFe.ToString(),
            CodigoVerificacao = par.ChaveNFe.CodigoVerificacao,
            Status = NotaFiscalStatus.Authorized
        }).ToList();
    }

    private async Task PersistAndLogAsync<T>(
        string operation,
        Func<Task<ApiResponse<T>>> persistAction)
    {
        try
        {
            var result = await persistAction();
            if (result.Success)
            {
                if (result.Data is PersistNotaFiscalBatchResponse batch)
                {
                    _logger.LogInformation(
                        "Main API persist/{Operation} succeeded: ProcessedCount={ProcessedCount} NotasRetornadas={Notas}",
                        operation,
                        batch.ProcessedCount,
                        batch.Notas.Count);
                }
                else if (result.Data is NotaFiscalResponse nota)
                {
                    _logger.LogInformation(
                        "Main API persist/{Operation} succeeded: NotaId={NotaId}",
                        operation,
                        nota.Id);
                }
                else
                {
                    _logger.LogInformation("Main API persist/{Operation} succeeded.", operation);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Main API persist/{Operation} failed: {Message}",
                    operation,
                    result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Main API persist/{Operation} threw an exception.", operation);
        }
    }

    private bool IsEnabled() => _integrationOptions.SyncPersistenceToMainApi;
}
