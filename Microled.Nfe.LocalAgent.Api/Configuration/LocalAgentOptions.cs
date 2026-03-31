namespace Microled.Nfe.LocalAgent.Api.Configuration;

public class LocalAgentOptions
{
    public const string SectionName = "LocalAgent";

    public int Port { get; set; } = 5278;

    public List<string> AllowedOrigins { get; set; } = [];

    public string? ApiKey { get; set; }
}
