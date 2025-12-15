namespace Microled.Nfe.Service.Infra.Configuration;

/// <summary>
/// Configuration options for NFS-e XML validation and export functionality
/// </summary>
public class NfeValidationOptions
{
    public const string SectionName = "NfeValidation";

    /// <summary>
    /// Enable validation/export mode. When true, generates XML files without calling WebService.
    /// </summary>
    public bool ValidateXmlAndRps { get; set; }

    /// <summary>
    /// Output directory for generated XML files (.RPS and SOAP)
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Prefix for generated file names
    /// </summary>
    public string FileNamePrefix { get; set; } = "NFE_SP";

    /// <summary>
    /// Include timestamp in file names
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;
}

