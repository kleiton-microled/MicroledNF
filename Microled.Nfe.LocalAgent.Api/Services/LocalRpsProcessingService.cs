using Microsoft.Extensions.Options;
using Microled.Nfe.LocalAgent.Api.Configuration;
using Microled.Nfe.LocalAgent.Api.Contracts;
using Microled.Nfe.Service.Application.DTOs;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;

namespace Microled.Nfe.LocalAgent.Api.Services;

public class LocalRpsProcessingService
{
    private readonly CertificateUnlockService _certificateUnlockService;
    private readonly IRpsBatchPreparationService _rpsBatchPreparationService;
    private readonly IRpsXmlValidationExportService _validationExportService;
    private readonly ISendRpsUseCase _sendRpsUseCase;
    private readonly NfeIntegrationOptions _integrationOptions;
    private readonly NfeValidationOptions _validationOptions;

    public LocalRpsProcessingService(
        CertificateUnlockService certificateUnlockService,
        IRpsBatchPreparationService rpsBatchPreparationService,
        IRpsXmlValidationExportService validationExportService,
        ISendRpsUseCase sendRpsUseCase,
        IOptions<NfeIntegrationOptions> integrationOptions,
        IOptions<NfeValidationOptions> validationOptions)
    {
        _certificateUnlockService = certificateUnlockService ?? throw new ArgumentNullException(nameof(certificateUnlockService));
        _rpsBatchPreparationService = rpsBatchPreparationService ?? throw new ArgumentNullException(nameof(rpsBatchPreparationService));
        _validationExportService = validationExportService ?? throw new ArgumentNullException(nameof(validationExportService));
        _sendRpsUseCase = sendRpsUseCase ?? throw new ArgumentNullException(nameof(sendRpsUseCase));
        _integrationOptions = integrationOptions.Value;
        _validationOptions = validationOptions.Value;
    }

    public async Task<LocalRpsProcessResponse> GenerateFilesAsync(
        SendRpsRequestDto request,
        CancellationToken cancellationToken)
    {
        return await GenerateFilesInternalAsync(request, ensureUnlocked: true, cancellationToken);
    }

    public async Task<LocalRpsProcessResponse> ProcessAsync(
        SendRpsRequestDto request,
        CancellationToken cancellationToken)
    {
        await _certificateUnlockService.UnlockAsync(cancellationToken);

        if (_validationOptions.ValidateXmlAndRps || !_integrationOptions.SendToWebService)
        {
            return await GenerateFilesInternalAsync(request, ensureUnlocked: false, cancellationToken);
        }

        var response = await _sendRpsUseCase.ExecuteAsync(request, cancellationToken);
        return MapSendResponse(response);
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
