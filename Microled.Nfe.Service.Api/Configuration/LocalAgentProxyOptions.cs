using Microsoft.Extensions.Options;

namespace Microled.Nfe.Service.Api.Configuration;

public class LocalAgentProxyOptions
{
    public const string SectionName = "LocalAgentProxy";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "http://127.0.0.1:5278";
}
