namespace Microled.Nfe.LocalAgent.Api.Configuration;

public class NfeIntegrationOptions
{
    public const string SectionName = "NfeIntegration";

    public bool SendToWebService { get; set; }

    public string? RpsOutputDirectory { get; set; }
}
