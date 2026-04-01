namespace Microled.Nfe.Service.Infra.Configuration;

/// <summary>
/// Configuration options for NFS-e service
/// </summary>
public class NfeServiceOptions
{
    public const string SectionName = "NfeService";

    /// <summary>
    /// Base URL for the NFe Web Service (e.g., "https://nfehomologacao.prefeitura.sp.gov.br/ws/lotenfe.asmx")
    /// If not set, will use ProductionEndpoint or TestEndpoint based on UseProduction flag
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Production endpoint URL (fallback if BaseUrl is not set)
    /// </summary>
    public string ProductionEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Test/Homologation endpoint URL (fallback if BaseUrl is not set)
    /// </summary>
    public string TestEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Production async send endpoint URL for schema v2 batch submission.
    /// When configured, SendRpsBatchAsync uses this endpoint instead of ProductionEndpoint.
    /// </summary>
    public string AsyncProductionEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Test/Homologation async send endpoint URL for schema v2 batch submission.
    /// When configured, SendRpsBatchAsync uses this endpoint instead of TestEndpoint.
    /// </summary>
    public string AsyncTestEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use production or test environment (used only if BaseUrl is not set)
    /// </summary>
    public bool UseProduction { get; set; } = false;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Environment name (Development, Homologation, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Default issuer CNPJ (CNPJ do prestador)
    /// </summary>
    public string DefaultIssuerCnpj { get; set; } = string.Empty;

    /// <summary>
    /// Default issuer Municipal Inscription (Inscrição Municipal do prestador)
    /// </summary>
    public string DefaultIssuerIm { get; set; } = string.Empty;

    /// <summary>
    /// Default sender CNPJ (CNPJ do remetente)
    /// </summary>
    public string DefaultCnpjRemetente { get; set; } = string.Empty;

    /// <summary>
    /// Schema version (e.g., "2.0")
    /// </summary>
    public string SchemaVersion { get; set; } = "2.0";

    /// <summary>
    /// Schema version for Versao field (e.g., "1" or "2")
    /// </summary>
    public string Versao { get; set; } = "1";

    /// <summary>
    /// Enable Schema V2 fields (ValorIPI, NBS, IBSCBS). When false, these fields are omitted from XML.
    /// </summary>
    public bool UseSchemaV2Fields { get; set; } = false;

    /// <summary>
    /// Enable logging of raw XML requests/responses
    /// </summary>
    public bool LogRawXml { get; set; } = false;

    /// <summary>
    /// Enable logging of sensitive data (CNPJ, CPF, values, keys)
    /// Only effective if LogRawXml is true
    /// </summary>
    public bool LogSensitiveData { get; set; } = false;

    /// <summary>
    /// Enable XML signature (ds:Signature) for PedidoEnvioLoteRPS
    /// Default: true (enabled)
    /// </summary>
    public bool EnableXmlSignature { get; set; } = true;

    /// <summary>
    /// XML signature algorithm: "SHA1" or "SHA256"
    /// Default: "SHA256"
    /// </summary>
    public string XmlSignatureAlgorithm { get; set; } = "SHA256";

    /// <summary>
    /// Certificate configuration
    /// </summary>
    public CertificateOptions Certificate { get; set; } = new();

    public string? GetPrimaryEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl;
        }

        var queryEndpoint = GetQueryEndpoint();
        if (!string.IsNullOrWhiteSpace(queryEndpoint))
        {
            return queryEndpoint;
        }

        return GetAsyncSendEndpoint();
    }

    public string? GetQueryEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl;
        }

        return UseProduction ? ProductionEndpoint : TestEndpoint;
    }

    public string? GetSendEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl;
        }

        var asyncEndpoint = GetAsyncSendEndpoint();
        if (!string.IsNullOrWhiteSpace(asyncEndpoint))
        {
            return asyncEndpoint;
        }

        return GetQueryEndpoint();
    }

    public bool UseAsyncSendContract()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(GetAsyncSendEndpoint());
    }

    private string GetAsyncSendEndpoint()
    {
        return UseProduction ? AsyncProductionEndpoint : AsyncTestEndpoint;
    }
}

public class CertificateOptions
{
    /// <summary>
    /// Certificate loading mode: "File" for A1 certificate (PFX file) or "Store" for A3 certificate (Windows Certificate Store)
    /// </summary>
    public string Mode { get; set; } = "File";

    /// <summary>
    /// Certificate file path (required if Mode = "File")
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Certificate password (required if Mode = "File" and certificate is password-protected)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Certificate store location (required if Mode = "Store")
    /// Options: "CurrentUser" or "LocalMachine"
    /// </summary>
    public string? StoreLocation { get; set; }

    /// <summary>
    /// Certificate store name (required if Mode = "Store")
    /// Options: "My", "TrustedPeople", etc.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Certificate thumbprint (required if Mode = "Store")
    /// </summary>
    public string? Thumbprint { get; set; }
}

