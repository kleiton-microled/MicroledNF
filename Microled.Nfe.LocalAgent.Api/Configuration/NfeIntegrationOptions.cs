namespace Microled.Nfe.LocalAgent.Api.Configuration;

public class NfeIntegrationOptions
{
    public const string SectionName = "NfeIntegration";

    public bool SendToWebService { get; set; }

    public string? RpsOutputDirectory { get; set; }

    /// <summary>
    /// Base URL of Microled.Nfe.Service.Api (e.g. http://localhost:5249).
    /// When set, SOAP results are forwarded for PostgreSQL persistence.
    /// </summary>
    public string? MainApiBaseUrl { get; set; }

    public bool SyncPersistenceToMainApi =>
        !string.IsNullOrWhiteSpace(MainApiBaseUrl);
}
