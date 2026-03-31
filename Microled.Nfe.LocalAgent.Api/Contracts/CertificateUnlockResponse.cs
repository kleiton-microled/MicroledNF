namespace Microled.Nfe.LocalAgent.Api.Contracts;

public class CertificateUnlockResponse
{
    public bool Success { get; set; }

    public string Thumbprint { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
