using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Mapping;
using Microled.Nfe.Service.Infra.Services;

namespace Microled.Nfe.Service.Infra.Storage;

/// <summary>
/// Exporta lote RPS para arquivos locais usando o mesmo <see cref="PedidoEnvioLoteRPS"/>, serialização e envelope SOAP do envio real.
/// </summary>
public class RpsXmlValidationExportService : IRpsXmlValidationExportService
{
    private readonly NfeValidationOptions _options;
    private readonly NfeServiceOptions _nfeOptions;
    private readonly IXmlSerializerService _xmlSerializer;
    private readonly ISoapEnvelopeBuilder _soapEnvelopeBuilder;
    private readonly IEnvioLoteRpsPedidoMapper _pedidoMapper;
    private readonly ILogger<RpsXmlValidationExportService> _logger;

    public RpsXmlValidationExportService(
        IOptions<NfeValidationOptions> options,
        IOptions<NfeServiceOptions> nfeOptions,
        IXmlSerializerService xmlSerializer,
        ISoapEnvelopeBuilder soapEnvelopeBuilder,
        IEnvioLoteRpsPedidoMapper pedidoMapper,
        ILogger<RpsXmlValidationExportService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _nfeOptions = nfeOptions.Value ?? throw new ArgumentNullException(nameof(nfeOptions));
        _xmlSerializer = xmlSerializer ?? throw new ArgumentNullException(nameof(xmlSerializer));
        _soapEnvelopeBuilder = soapEnvelopeBuilder ?? throw new ArgumentNullException(nameof(soapEnvelopeBuilder));
        _pedidoMapper = pedidoMapper ?? throw new ArgumentNullException(nameof(pedidoMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationExportResult> ExportAsync(RpsBatch batch, CancellationToken cancellationToken)
    {
        return await ExportAsync(batch, null, cancellationToken);
    }

    public async Task<ValidationExportResult> ExportAsync(
        RpsBatch batch,
        string? outputDirectory,
        CancellationToken cancellationToken)
    {
        if (batch == null)
        {
            throw new ArgumentNullException(nameof(batch));
        }

        if (batch.RpsList.Count == 0)
        {
            throw new ArgumentException("RPS batch cannot be empty", nameof(batch));
        }

        var resolvedOutputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? _options.OutputDirectory
            : outputDirectory;

        if (string.IsNullOrWhiteSpace(resolvedOutputDirectory))
        {
            throw new InvalidOperationException("NfeValidation:OutputDirectory must be configured");
        }

        Directory.CreateDirectory(resolvedOutputDirectory);

        var timestamp = _options.IncludeTimestamp
            ? DateTime.Now.ToString("yyyyMMdd_HHmmss")
            : string.Empty;
        var guid = Guid.NewGuid().ToString("N")[..8];
        var suffix = string.IsNullOrEmpty(timestamp) ? guid : $"{timestamp}_{guid}";

        var rpsFileName = $"{_options.FileNamePrefix}_LoteRps_{suffix}.RPS";
        var soapFileName = $"{_options.FileNamePrefix}_SoapEnvioLoteRps_{suffix}.xml";

        var rpsFilePath = Path.Combine(resolvedOutputDirectory, rpsFileName);
        var soapFilePath = Path.Combine(resolvedOutputDirectory, soapFileName);

        var pedido = _pedidoMapper.MapFromBatch(batch);

        var xmlContent = _xmlSerializer.SerializePedidoEnvioLoteRPS(pedido);
        _logger.LogDebug("Serialized PedidoEnvioLoteRPS XML (length: {Length})", xmlContent.Length);

        await File.WriteAllTextAsync(rpsFilePath, xmlContent, cancellationToken);
        _logger.LogInformation("Saved RPS XML file: {FilePath}", rpsFilePath);

        var versaoSchema = int.Parse(_nfeOptions.Versao.Replace(".", ""));
        var soapEnvelope = _soapEnvelopeBuilder.BuildEnvioLoteRPS(xmlContent, versaoSchema);
        if (_nfeOptions.UseAsyncSendContract())
        {
            soapEnvelope = NormalizeAsyncSendEnvelope(soapEnvelope);
        }

        _logger.LogDebug("Built SOAP envelope (length: {Length})", soapEnvelope.Length);

        await File.WriteAllTextAsync(soapFilePath, soapEnvelope, cancellationToken);
        _logger.LogInformation("Saved SOAP envelope file: {FilePath}", soapFilePath);

        return new ValidationExportResult
        {
            RpsFilePath = rpsFilePath,
            SoapFilePath = soapFilePath
        };
    }

    /// <summary>Mesma normalização que o envio HTTP quando <c>UseAsyncSendContract</c> está ativo.</summary>
    private static string NormalizeAsyncSendEnvelope(string soapEnvelope)
    {
        return soapEnvelope
            .Replace("<VersaoSchema>", "<versaoSchema>", StringComparison.Ordinal)
            .Replace("</VersaoSchema>", "</versaoSchema>", StringComparison.Ordinal);
    }
}
