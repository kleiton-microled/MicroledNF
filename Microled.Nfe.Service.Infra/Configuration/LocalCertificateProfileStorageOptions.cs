namespace Microled.Nfe.Service.Infra.Configuration;

/// <summary>
/// Local storage settings for certificate selection metadata.
/// </summary>
public class LocalCertificateProfileStorageOptions
{
    public string DataDirectory { get; set; } = string.Empty;

    public string FileName { get; set; } = "company-certificate-profiles.json";
}
