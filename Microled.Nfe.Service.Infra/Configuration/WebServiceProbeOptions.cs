namespace Microled.Nfe.Service.Infra.Configuration;

/// <summary>
/// Configuration options for Web Service probe/diagnostic functionality
/// </summary>
public class WebServiceProbeOptions
{
    public const string SectionName = "WebServiceProbe";

    /// <summary>
    /// Enable probe mode. When true, console will only run diagnostics and exit.
    /// </summary>
    public bool EnableProbe { get; set; }

    /// <summary>
    /// List of candidate URLs to test
    /// </summary>
    public List<string> CandidateUrls { get; set; } = new();

    /// <summary>
    /// Timeout in seconds for each probe request
    /// </summary>
    public int TimeoutSeconds { get; set; } = 20;
}

