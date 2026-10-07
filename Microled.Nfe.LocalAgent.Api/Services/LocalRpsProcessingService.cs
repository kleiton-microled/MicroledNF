using Microsoft.Extensions.Options;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.LocalAgent.Api.Contracts;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Repositories;

namespace Microled.Nfe.LocalAgent.Api.Services;

public class LocalRpsProcessingService
{
    private readonly CertificateUnlockService _certificateUnlockService;
    private readonly IRpsBatchPreparationService _rpsBatchPreparationService;
    private readonly IRpsXmlValidationExportService _validationExportService;
    private readonly ISendRpsUseCase _sendRpsUseCase;
    private readonly LocalAgentNotaFiscalSyncService _notaFiscalSyncService;
    private readonly PendingRpsQueueStore _pendingQueue;
    private readonly IAccessRpsRepository _accessRpsRepository;
    private readonly IMainApiNotaFiscalClient _mainApiClient;
    private readonly NfeIntegrationOptions _integrationOptions;
    private readonly NfeValidationOptions _validationOptions;
    private readonly ILogger<LocalRpsProcessingService> _logger;

    public LocalRpsProcessingService(
        CertificateUnlockService certificateUnlockService,
        IRpsBatchPreparationService rpsBatchPreparationService,
        IRpsXmlValidationExportService validationExportService,
        ISendRpsUseCase sendRpsUseCase,
        LocalAgentNotaFiscalSyncService notaFiscalSyncService,
        PendingRpsQueueStore pendingQueue,
        IAccessRpsRepository accessRpsRepository,
        IMainApiNotaFiscalClient mainApiClient,
        IOptions<NfeIntegrationOptions> integrationOptions,
        IOptions<NfeValidationOptions> validationOptions,
        ILogger<LocalRpsProcessingService> logger)
    {
        _certificateUnlockService = certificateUnlockService ?? throw new ArgumentNullException(nameof(certificateUnlockService));
        _rpsBatchPreparationService = rpsBatchPreparationService ?? throw new ArgumentNullException(nameof(rpsBatchPreparationService));
        _validationExportService = validationExportService ?? throw new ArgumentNullException(nameof(validationExportService));
        _sendRpsUseCase = sendRpsUseCase ?? throw new ArgumentNullException(nameof(sendRpsUseCase));
        _notaFiscalSyncService = notaFiscalSyncService ?? throw new ArgumentNullException(nameof(notaFiscalSyncService));
        _pendingQueue = pendingQueue ?? throw new ArgumentNullException(nameof(pendingQueue));
        _accessRpsRepository = accessRpsRepository ?? throw new ArgumentNullException(nameof(accessRpsRepository));
        _mainApiClient = mainApiClient ?? throw new ArgumentNullException(nameof(mainApiClient));
        _integrationOptions = integrationOptions.Value;
        _validationOptions = validationOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LocalRpsProcessResponse> GenerateFilesAsync(
        SendRpsRequestDto request,
        CancellationToken cancellationToken)
    {
        return await GenerateFilesInternalAsync(request, ensureUnlocked: true, cancellationToken);
    }

    public async Task<LocalRpsProcessResponse> ProcessAsync(
        SendRpsRequestDto request,
        CancellationToken cancellationToken,
        Guid? notaId = null)
    {
        await _certificateUnlockService.UnlockAsync(cancellationToken);

        if (!_integrationOptions.SendToWebService)
        {
            return await GenerateFilesInternalAsync(request, ensureUnlocked: false, cancellationToken);
        }

        var response = await _sendRpsUseCase.ExecuteAsync(request, cancellationToken);
        await _notaFiscalSyncService.SyncSendResultAsync(request, response, cancellationToken, notaId);
        var mapped = MapSendResponse(response);

        if (_validationOptions.ValidateXmlAndRps)
        {
            var batch = _rpsBatchPreparationService.PrepareSignedBatch(request);
            var export = await _validationExportService.ExportAsync(
                batch,
                ResolveOutputDirectory(),
                cancellationToken);
            mapped.LocalFilePath = export.RpsFilePath;
            mapped.SoapFilePath = export.SoapFilePath;
        }

        return mapped;
    }

    private async Task<LocalRpsProcessResponse> GenerateFilesInternalAsync(
        SendRpsRequestDto request,
        bool ensureUnlocked,
        CancellationToken cancellationToken)
    {
        if (ensureUnlocked)
        {
            await _certificateUnlockService.UnlockAsync(cancellationToken);
        }

        var batch = _rpsBatchPreparationService.PrepareSignedBatch(request);
        var export = await _validationExportService.ExportAsync(
            batch,
            ResolveOutputDirectory(),
            cancellationToken);

        return new LocalRpsProcessResponse
        {
            Success = true,
            IsSentToWebService = false,
            LocalFilePath = export.RpsFilePath,
            SoapFilePath = export.SoapFilePath,
            Message = "Arquivos gerados com sucesso."
        };
    }

    public async Task<QueuePendingRpsResponse> QueuePendingAsync(
        QueuePendingRpsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Request?.RpsList is not { Count: > 0 })
        {
            return new QueuePendingRpsResponse
            {
                Success = false,
                Message = "Informe um RPS para importar.",
                Errors = ["RpsList is empty."]
            };
        }

        await _certificateUnlockService.UnlockAsync(cancellationToken);
        var generated = await GenerateFilesInternalAsync(request.Request, ensureUnlocked: false, cancellationToken);
        if (!generated.Success)
        {
            return new QueuePendingRpsResponse
            {
                Success = false,
                Message = generated.Message,
                Errors = generated.Errors.Select(e => e.Descricao ?? string.Empty).ToList()
            };
        }

        var rps = request.Request.RpsList[0];
        var create = await _mainApiClient.CreateAsync(
            new CreateNotaFiscalRequest
            {
                CriadoPor = "localagent",
                NumeroRps = rps.NumeroRps.ToString(),
                SerieRps = rps.SerieRps,
                InscricaoPrestador = rps.InscricaoPrestador.ToString(),
                CnpjPrestador = request.Request.Prestador.CpfCnpj,
                CpfCnpjTomador = rps.Tomador?.CpfCnpj,
                DataEmissao = new DateTimeOffset(rps.DataEmissao.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            },
            cancellationToken);

        if (!create.Success || create.Data is null)
        {
            return new QueuePendingRpsResponse
            {
                Success = false,
                Message = create.Message ?? "Nao foi possivel gravar a nota como a processar.",
                Errors = [create.Message ?? "Create nota failed."]
            };
        }

        _pendingQueue.Save(create.Data.Id, request.Request);

        if (request.RecordIds.Count == 1)
        {
            await UpdateAccessTaxValuesAsync(
                request.RecordIds[0],
                rps,
                request.BaseCalculoFederal,
                request.ValorLiquido,
                cancellationToken);
        }

        if (request.RecordIds.Count > 0)
        {
            await _accessRpsRepository.MarkAsGeneratedAsync(
                request.RecordIds.Select(id => new RpsRecord { Id = id }),
                cancellationToken);
        }

        return new QueuePendingRpsResponse
        {
            Success = true,
            NotaId = create.Data.Id,
            Message = "RPS importado e aguardando processamento."
        };
    }

    public async Task<LocalRpsProcessResponse> ProcessQueuedAsync(Guid notaId, CancellationToken cancellationToken)
    {
        var payload = _pendingQueue.Load(notaId);
        if (payload is null)
        {
            return new LocalRpsProcessResponse
            {
                Success = false,
                Message = "Nao foi encontrado o payload do RPS pendente para envio.",
                Errors =
                [
                    new EventoDto { Codigo = 0, Descricao = $"Payload ausente para {notaId}." }
                ]
            };
        }

        return await ProcessAsync(payload, cancellationToken, notaId);
    }

    /// <summary>
    /// Devolve ao Access os impostos calculados pelo servico. Falha aqui nao impede a importacao
    /// (a nota ja foi gravada como a processar); apenas registra o erro no log.
    /// </summary>
    private async Task UpdateAccessTaxValuesAsync(
        int recordId,
        RpsDto rps,
        decimal? baseCalculoFederal,
        decimal? valorLiquido,
        CancellationToken cancellationToken)
    {
        try
        {
            await _accessRpsRepository.UpdateTaxValuesAsync(
                recordId,
                new RpsTaxValues
                {
                    BaseCalculo = baseCalculoFederal,
                    ValorPIS = rps.Tributos?.ValorPIS ?? rps.ValorPIS,
                    ValorCOFINS = rps.Tributos?.ValorCOFINS ?? rps.ValorCOFINS,
                    ValorIR = rps.Tributos?.ValorIR ?? rps.ValorIR,
                    ValorCSLL = rps.Tributos?.ValorCSLL ?? rps.ValorCSLL,
                    ValorLiquido = valorLiquido ?? rps.Tributos?.ValorFinalCobrado
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gravar impostos calculados no Access. RecordId={RecordId}", recordId);
        }
    }

    private LocalRpsProcessResponse MapSendResponse(SendRpsResponseDto response)
    {
        return new LocalRpsProcessResponse
        {
            Success = response.Sucesso,
            IsSentToWebService = true,
            Protocol = response.Protocolo,
            Message = response.Sucesso
                ? "Lote enviado ao WebService com sucesso."
                : "O processamento retornou erros no envio ao WebService.",
            Warnings = response.Alertas,
            Errors = response.Erros,
            NfeRpsKeys = response.ChavesNFeRPS
        };
    }

    private string? ResolveOutputDirectory()
    {
        return string.IsNullOrWhiteSpace(_integrationOptions.RpsOutputDirectory)
            ? null
            : _integrationOptions.RpsOutputDirectory;
    }
}
