namespace Microled.Nfe.Service.Infra.Storage;

/// <summary>
/// Result of XML validation export operation
/// </summary>
public class ValidationExportResult
{
    /// <summary>
    /// Full path to the generated .RPS file
    /// </summary>
    public string RpsFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the generated SOAP envelope XML file
    /// </summary>
    public string SoapFilePath { get; set; } = string.Empty;
}

