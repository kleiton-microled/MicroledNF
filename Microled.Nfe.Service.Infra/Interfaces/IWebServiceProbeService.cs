namespace Microled.Nfe.Service.Infra.Interfaces;

/// <summary>
/// Probes candidate SOAP endpoints and identifies the most viable one.
/// </summary>
public interface IWebServiceProbeService
{
    Task<WebServiceProbeResponse> ProbeAsync(
        IReadOnlyCollection<string>? candidateUrls,
        CancellationToken cancellationToken);

    Task RunAsync(CancellationToken cancellationToken);
}

public class WebServiceProbeResponse
{
    public List<WebServiceProbeResultDto> Results { get; set; } = [];

    public string? BestCandidateUrl { get; set; }
}

public class WebServiceProbeResultDto
{
    public string Url { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }

    public bool IsSoap { get; set; }

    public bool IsSoapFault { get; set; }

    public long ElapsedMs { get; set; }

    public string? ErrorMessage { get; set; }
}
