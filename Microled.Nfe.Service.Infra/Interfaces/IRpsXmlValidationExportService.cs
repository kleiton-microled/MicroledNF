using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Infra.Storage;

namespace Microled.Nfe.Service.Infra.Interfaces;

/// <summary>
/// Service for exporting RPS batch to XML files for validation (without calling WebService)
/// </summary>
public interface IRpsXmlValidationExportService
{
    /// <summary>
    /// Exports RPS batch to XML files (.RPS and SOAP envelope)
    /// </summary>
    /// <param name="batch">RPS batch to export</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing paths to generated files</returns>
    Task<ValidationExportResult> ExportAsync(RpsBatch batch, CancellationToken cancellationToken);
}

