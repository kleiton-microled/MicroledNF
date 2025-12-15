using System.Diagnostics;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Infra.Configuration;

namespace Microled.Nfe.Service.Console;

/// <summary>
/// Service interface for Web Service probe/diagnostic functionality
/// </summary>
public interface IWebServiceProbeService
{
    /// <summary>
    /// Runs the probe against all configured candidate URLs
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Service that probes multiple Web Service URLs to identify which ones are functional
/// </summary>
public class WebServiceProbeService : IWebServiceProbeService
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NfeNamespace = "http://www.prefeitura.sp.gov.br/nfe";

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebServiceProbeService> _logger;
    private readonly WebServiceProbeOptions _options;

    public WebServiceProbeService(
        IHttpClientFactory httpClientFactory,
        IOptions<WebServiceProbeOptions> options,
        ILogger<WebServiceProbeService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("========== WEB SERVICE PROBE ==========");
        _logger.LogInformation("Testing {Count} candidate URLs...", _options.CandidateUrls.Count);
        _logger.LogInformation("");

        if (_options.CandidateUrls.Count == 0)
        {
            _logger.LogWarning("No candidate URLs configured. Please add URLs to WebServiceProbe:CandidateUrls in appsettings.json");
            return;
        }

        var results = new List<ProbeResult>();

        for (int i = 0; i < _options.CandidateUrls.Count; i++)
        {
            var url = _options.CandidateUrls[i];
            _logger.LogInformation("[{Index}] Testing URL: {Url}", i + 1, url);

            var result = await ProbeUrlAsync(url, cancellationToken);
            results.Add(result);

            LogResult(i + 1, result);
            _logger.LogInformation("");
        }

        _logger.LogInformation("========================================");
        _logger.LogInformation("");

        // Analyze results and suggest functional endpoints
        // SOAP Fault = endpoint válido! (mesmo com HTTP error)
        var functionalUrls = results
            .Where(r => r.Status == ProbeStatus.SoapOk || r.Status == ProbeStatus.SoapFault)
            .ToList();
        
        // Also check 403 responses that might be SOAP (some servers return SOAP with 403)
        var potentialSoapUrls = results
            .Where(r => r.HttpStatusCode == 403 && !string.IsNullOrEmpty(r.ResponseContent) && IsSoapEnvelope(r.ResponseContent))
            .ToList();

        if (functionalUrls.Any())
        {
            _logger.LogInformation("SUGESTÃO:");
            _logger.LogInformation("Endpoint(s) funcional(is) encontrado(s):");
            foreach (var result in functionalUrls)
            {
                var statusIcon = result.Status == ProbeStatus.SoapOk ? "✅" : "⚠️";
                _logger.LogInformation("{Icon} {Url}", statusIcon, result.Url);
                if (result.Status == ProbeStatus.SoapFault)
                {
                    _logger.LogInformation("   (SOAP Fault = endpoint correto, payload errado - URL válida!)");
                }
            }
        }
        else if (potentialSoapUrls.Any())
        {
            _logger.LogInformation("ATENÇÃO:");
            _logger.LogInformation("Endpoint(s) que retornaram 403 mas podem ser SOAP válidos:");
            foreach (var result in potentialSoapUrls)
            {
                _logger.LogInformation("⚠️ {Url}", result.Url);
                _logger.LogInformation("   (403 Forbidden mas resposta parece SOAP - pode precisar de certificado/autenticação)");
            }
            _logger.LogInformation("");
            _logger.LogInformation("Recomendação: Teste essas URLs com certificado real.");
        }
        else
        {
            _logger.LogWarning("Nenhum endpoint funcional encontrado. Verifique:");
            _logger.LogWarning("  - Conectividade de rede");
            _logger.LogWarning("  - URLs configuradas corretamente");
            _logger.LogWarning("  - Firewall/proxy");
            _logger.LogWarning("  - Alguns endpoints podem exigir certificado para responder SOAP");
        }

        _logger.LogInformation("");
        _logger.LogInformation("========================================");
    }

    private async Task<ProbeResult> ProbeUrlAsync(string url, CancellationToken cancellationToken)
    {
        var result = new ProbeResult
        {
            Url = url,
            StartTime = DateTime.UtcNow
        };

        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                result.Status = ProbeStatus.InvalidUrl;
                result.ErrorMessage = "Invalid URL format";
                result.Duration = TimeSpan.Zero;
                return result;
            }

            // Create a test SOAP envelope (minimal, safe request)
            var soapEnvelope = BuildTestSoapEnvelope();

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            request.Content.Headers.Add("SOAPAction", $"{NfeNamespace}/ConsultaNFe");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await _httpClient.SendAsync(request, cts.Token);
                stopwatch.Stop();

                result.HttpStatusCode = (int)response.StatusCode;
                result.Duration = stopwatch.Elapsed;

                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
                result.ResponseLength = responseContent.Length;
                result.ResponseContent = responseContent; // Store for analysis

                // Analyze response - check for SOAP even on non-200 status codes
                // Some servers return SOAP Faults with 200, others with 400/403/500
                if (IsSoapEnvelope(responseContent))
                {
                    if (HasSoapFault(responseContent))
                    {
                        result.Status = ProbeStatus.SoapFault;
                        result.ErrorMessage = ExtractSoapFaultMessage(responseContent);
                        // SOAP Fault = endpoint válido! (mesmo com HTTP error)
                        if (!response.IsSuccessStatusCode)
                        {
                            result.ErrorMessage += $" (HTTP {response.StatusCode})";
                        }
                    }
                    else
                    {
                        result.Status = ProbeStatus.SoapOk;
                        if (!response.IsSuccessStatusCode)
                        {
                            result.ErrorMessage = $"HTTP {response.StatusCode} but valid SOAP response";
                        }
                    }
                }
                else if (response.IsSuccessStatusCode)
                {
                    result.Status = ProbeStatus.HttpOkButNotSoap;
                    result.ErrorMessage = "Response is not a valid SOAP envelope";
                }
                else
                {
                    // HTTP error and not SOAP
                    result.Status = ProbeStatus.HttpError;
                    result.ErrorMessage = $"HTTP {response.StatusCode}";
                    
                    // Try to extract useful info from response body
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        var preview = responseContent.Length > 200 
                            ? responseContent.Substring(0, 200) + "..." 
                            : responseContent;
                        result.ErrorMessage += $": {preview.Replace("\n", " ").Replace("\r", "")}";
                    }
                }
            }
            catch (TaskCanceledException) when (cts.Token.IsCancellationRequested)
            {
                stopwatch.Stop();
                result.Status = ProbeStatus.Timeout;
                result.Duration = stopwatch.Elapsed;
                result.ErrorMessage = $"Request timeout after {_options.TimeoutSeconds}s";
            }
        }
        catch (HttpRequestException ex)
        {
            result.Status = ProbeStatus.ConnectionError;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTime.UtcNow - result.StartTime;
        }
        catch (Exception ex)
        {
            result.Status = ProbeStatus.UnknownError;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTime.UtcNow - result.StartTime;
        }

        return result;
    }

    private string BuildTestSoapEnvelope()
    {
        // Minimal SOAP envelope for testing - safe, no real data
        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"">
    <soap:Body>
        <ConsultaNFe xmlns=""{NfeNamespace}"">
            <MensagemXML><![CDATA[
                <PedidoConsultaNFe xmlns=""http://www.prefeitura.sp.gov.br/nfe"">
                    <Cabecalho Versao=""2"" xmlns="""">
                        <CPFCNPJRemetente>00000000000000</CPFCNPJRemetente>
                    </Cabecalho>
                </PedidoConsultaNFe>
            ]]></MensagemXML>
        </ConsultaNFe>
    </soap:Body>
</soap:Envelope>";

        return soapEnvelope;
    }

    private bool IsSoapEnvelope(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        try
        {
            var doc = XDocument.Parse(content);
            var ns = XNamespace.Get(SoapNamespace);
            return doc.Root?.Name == ns + "Envelope";
        }
        catch
        {
            return false;
        }
    }

    private bool HasSoapFault(string soapContent)
    {
        try
        {
            var doc = XDocument.Parse(soapContent);
            var ns = XNamespace.Get(SoapNamespace);
            return doc.Descendants(ns + "Fault").Any();
        }
        catch
        {
            return false;
        }
    }

    private string ExtractSoapFaultMessage(string soapContent)
    {
        try
        {
            var doc = XDocument.Parse(soapContent);
            var ns = XNamespace.Get(SoapNamespace);
            var fault = doc.Descendants(ns + "Fault").FirstOrDefault();
            if (fault != null)
            {
                var faultCode = fault.Element(ns + "faultcode")?.Value;
                var faultString = fault.Element(ns + "faultstring")?.Value;
                return $"{faultCode}: {faultString}";
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return "SOAP Fault detected";
    }

    private void LogResult(int index, ProbeResult result)
    {
        var statusText = result.Status switch
        {
            ProbeStatus.SoapOk => "✅ SOAP OK",
            ProbeStatus.SoapFault => "⚠️ SOAP Fault (endpoint válido!)",
            ProbeStatus.HttpOkButNotSoap => "❌ HTTP OK mas não é SOAP",
            ProbeStatus.HttpError => $"❌ HTTP Error {result.HttpStatusCode}",
            ProbeStatus.Timeout => "❌ Timeout",
            ProbeStatus.ConnectionError => "❌ Connection Error",
            ProbeStatus.InvalidUrl => "❌ Invalid URL",
            ProbeStatus.UnknownError => "❌ Unknown Error",
            _ => "❓ Unknown"
        };

        _logger.LogInformation("    Status: {Status}", statusText);
        _logger.LogInformation("    HTTP: {HttpStatus}", 
            result.HttpStatusCode.HasValue ? $"{result.HttpStatusCode} {GetHttpStatusText(result.HttpStatusCode.Value)}" : "N/A");
        _logger.LogInformation("    Time: {Duration} ms", result.Duration.TotalMilliseconds.ToString("F0"));
        
        if (result.Status == ProbeStatus.SoapFault || result.Status == ProbeStatus.SoapOk)
        {
            _logger.LogInformation("    SOAP: {SoapStatus}", result.Status == ProbeStatus.SoapFault ? "Fault (mas endpoint válido!)" : "OK");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            _logger.LogInformation("    Error: {ErrorMessage}", result.ErrorMessage);
        }
    }

    private string GetHttpStatusText(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => "Unknown"
        };
    }

    private class ProbeResult
    {
        public string Url { get; set; } = string.Empty;
        public ProbeStatus Status { get; set; }
        public int? HttpStatusCode { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public int ResponseLength { get; set; }
        public string? ResponseContent { get; set; }
        public DateTime StartTime { get; set; }
    }

    private enum ProbeStatus
    {
        SoapOk,
        SoapFault,
        HttpOkButNotSoap,
        HttpError,
        Timeout,
        ConnectionError,
        InvalidUrl,
        UnknownError
    }
}

