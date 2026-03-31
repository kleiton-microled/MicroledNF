using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Probes multiple Web Service URLs and evaluates whether they behave like valid SOAP endpoints.
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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WebServiceProbeResponse> ProbeAsync(
        IReadOnlyCollection<string>? candidateUrls,
        CancellationToken cancellationToken)
    {
        var urls = (candidateUrls == null || candidateUrls.Count == 0
                ? _options.CandidateUrls
                : candidateUrls.ToList())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var response = new WebServiceProbeResponse();

        foreach (var url in urls)
        {
            var result = await ProbeUrlAsync(url, cancellationToken);
            response.Results.Add(new WebServiceProbeResultDto
            {
                Url = result.Url,
                HttpStatusCode = result.HttpStatusCode,
                IsSoap = result.IsSoap,
                IsSoapFault = result.IsSoapFault,
                ElapsedMs = (long)Math.Round(result.Duration.TotalMilliseconds),
                ErrorMessage = result.ErrorMessage
            });
        }

        response.BestCandidateUrl = SelectBestCandidate(response.Results);
        return response;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("========== WEB SERVICE PROBE ==========");

        if (_options.CandidateUrls.Count == 0)
        {
            _logger.LogWarning("No candidate URLs configured. Please add URLs to WebServiceProbe:CandidateUrls in appsettings.json");
            return;
        }

        var probeResponse = await ProbeAsync(_options.CandidateUrls, cancellationToken);

        _logger.LogInformation("Testing {Count} candidate URLs...", probeResponse.Results.Count);
        _logger.LogInformation(string.Empty);

        for (var index = 0; index < probeResponse.Results.Count; index++)
        {
            var result = probeResponse.Results[index];
            _logger.LogInformation("[{Index}] Testing URL: {Url}", index + 1, result.Url);
            LogResult(result);
            _logger.LogInformation(string.Empty);
        }

        _logger.LogInformation("========================================");
        _logger.LogInformation(string.Empty);

        var functionalUrls = probeResponse.Results
            .Where(result => result.IsSoap)
            .ToList();

        if (functionalUrls.Count > 0)
        {
            _logger.LogInformation("SUGESTAO:");
            _logger.LogInformation("Endpoint(s) funcional(is) encontrado(s):");

            foreach (var result in functionalUrls)
            {
                var statusIcon = result.IsSoapFault ? "⚠️" : "✅";
                _logger.LogInformation("{Icon} {Url}", statusIcon, result.Url);

                if (result.IsSoapFault)
                {
                    _logger.LogInformation("   (SOAP Fault = endpoint correto, payload errado - URL valida!)");
                }
            }
        }
        else
        {
            _logger.LogWarning("Nenhum endpoint SOAP funcional encontrado. Verifique conectividade, URLs, proxy/firewall e necessidade de certificado.");
        }

        if (!string.IsNullOrWhiteSpace(probeResponse.BestCandidateUrl))
        {
            _logger.LogInformation(string.Empty);
            _logger.LogInformation("Melhor candidato: {BestCandidateUrl}", probeResponse.BestCandidateUrl);
        }

        _logger.LogInformation(string.Empty);
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
                result.ResponseContent = responseContent;
                result.IsSoap = IsSoapEnvelope(responseContent);
                result.IsSoapFault = result.IsSoap && HasSoapFault(responseContent);

                if (result.IsSoapFault)
                {
                    result.Status = ProbeStatus.SoapFault;
                    result.ErrorMessage = ExtractSoapFaultMessage(responseContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        result.ErrorMessage += $" (HTTP {response.StatusCode})";
                    }
                }
                else if (result.IsSoap)
                {
                    result.Status = ProbeStatus.SoapOk;
                    if (!response.IsSuccessStatusCode)
                    {
                        result.ErrorMessage = $"HTTP {response.StatusCode} but valid SOAP response";
                    }
                }
                else if (response.IsSuccessStatusCode)
                {
                    result.Status = ProbeStatus.HttpOkButNotSoap;
                    result.ErrorMessage = "Response is not a valid SOAP envelope";
                }
                else
                {
                    result.Status = ProbeStatus.HttpError;
                    result.ErrorMessage = $"HTTP {response.StatusCode}";

                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        var preview = responseContent.Length > 200
                            ? responseContent[..200] + "..."
                            : responseContent;

                        result.ErrorMessage += $": {preview.Replace("\n", " ").Replace("\r", string.Empty)}";
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

    private static string? SelectBestCandidate(IReadOnlyCollection<WebServiceProbeResultDto> results)
    {
        var best = results
            .OrderByDescending(result => result.IsSoap)
            .ThenBy(result => result.IsSoapFault)
            .ThenBy(result => result.HttpStatusCode ?? int.MaxValue)
            .ThenBy(result => result.ElapsedMs)
            .FirstOrDefault();

        return best?.IsSoap == true ? best.Url : null;
    }

    private static string BuildTestSoapEnvelope()
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
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
    }

    private static bool IsSoapEnvelope(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

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

    private static bool HasSoapFault(string soapContent)
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

    private static string ExtractSoapFaultMessage(string soapContent)
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
        }

        return "SOAP Fault detected";
    }

    private void LogResult(WebServiceProbeResultDto result)
    {
        var statusText = result switch
        {
            { IsSoap: true, IsSoapFault: false } => "✅ SOAP OK",
            { IsSoap: true, IsSoapFault: true } => "⚠️ SOAP Fault (endpoint valido!)",
            { HttpStatusCode: not null } => $"❌ HTTP {result.HttpStatusCode}",
            _ => "❌ Erro"
        };

        _logger.LogInformation("    Status: {Status}", statusText);
        _logger.LogInformation("    HTTP: {HttpStatus}", result.HttpStatusCode?.ToString() ?? "N/A");
        _logger.LogInformation("    Time: {Duration} ms", result.ElapsedMs);

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            _logger.LogInformation("    Error: {ErrorMessage}", result.ErrorMessage);
        }
    }

    private sealed class ProbeResult
    {
        public string Url { get; set; } = string.Empty;

        public ProbeStatus Status { get; set; }

        public int? HttpStatusCode { get; set; }

        public TimeSpan Duration { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ResponseContent { get; set; }

        public bool IsSoap { get; set; }

        public bool IsSoapFault { get; set; }

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
