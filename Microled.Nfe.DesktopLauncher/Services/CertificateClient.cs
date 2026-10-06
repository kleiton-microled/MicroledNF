using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Microled.Nfe.DesktopLauncher.Services;

public sealed class CertificateClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<IReadOnlyList<CertificateItem>> ListAsync(string agentBaseUrl, CancellationToken cancellationToken)
    {
        var url = agentBaseUrl.TrimEnd('/') + "/api/local/certificates";
        var items = await _http.GetFromJsonAsync<List<CertificateItem>>(url, cancellationToken);
        return items ?? [];
    }

    public async Task SelectAsync(string agentBaseUrl, CertificateItem certificate, CancellationToken cancellationToken)
    {
        var url = agentBaseUrl.TrimEnd('/') + "/api/local/certificates/select";
        using var response = await _http.PostAsJsonAsync(
            url,
            new
            {
                thumbprint = certificate.Thumbprint,
                storeLocation = certificate.StoreLocation,
                storeName = certificate.StoreName
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class CertificateItem
{
    [JsonPropertyName("thumbprint")]
    public string Thumbprint { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("simpleName")]
    public string? SimpleName { get; set; }

    [JsonPropertyName("storeLocation")]
    public string StoreLocation { get; set; } = "CurrentUser";

    [JsonPropertyName("storeName")]
    public string StoreName { get; set; } = "My";

    public override string ToString()
    {
        var name = string.IsNullOrWhiteSpace(SimpleName) ? Subject : SimpleName;
        var shortThumb = Thumbprint.Length > 8 ? Thumbprint[..8] : Thumbprint;
        return $"{name} ({shortThumb}…)";
    }
}
