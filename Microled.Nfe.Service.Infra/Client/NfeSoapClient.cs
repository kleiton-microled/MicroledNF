using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Application.Interfaces;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.ValueObjects;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Exceptions;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.Services;
using Microled.Nfe.Service.Infra.XmlSchemas;
using DomainEntities = Microled.Nfe.Service.Domain.Entities;

namespace Microled.Nfe.Service.Infra.Client;

/// <summary>
/// SOAP client for São Paulo City Hall NFS-e Web Service
/// </summary>
public class NfeSoapClient : INfeGateway
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NfeNamespace = "http://www.prefeitura.sp.gov.br/nfe";
    private const string NfeTiposNamespace = "http://www.prefeitura.sp.gov.br/nfe/tipos";

    private readonly HttpClient _httpClient;
    private readonly ILogger<NfeSoapClient> _logger;
    private readonly NfeServiceOptions _options;
    private readonly IXmlSerializerService _xmlSerializer;
    private readonly ISoapEnvelopeBuilder _soapEnvelopeBuilder;
    private readonly ICertificateProvider? _certificateProvider;
    private readonly IRpsSignatureService? _rpsSignatureService;
    private readonly IServiceTaxRateProvider _serviceTaxRateProvider;
    private readonly ConsultaNfeXsdValidator? _consultaNfeXsdValidator;

    public NfeSoapClient(
        HttpClient httpClient,
        ILogger<NfeSoapClient> logger,
        IOptions<NfeServiceOptions> options,
        IXmlSerializerService xmlSerializer,
        ISoapEnvelopeBuilder soapEnvelopeBuilder,
        ICertificateProvider? certificateProvider = null,
        IRpsSignatureService? rpsSignatureService = null,
        IServiceTaxRateProvider? serviceTaxRateProvider = null,
        ConsultaNfeXsdValidator? consultaNfeXsdValidator = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
        _xmlSerializer = xmlSerializer ?? throw new ArgumentNullException(nameof(xmlSerializer));
        _soapEnvelopeBuilder = soapEnvelopeBuilder ?? throw new ArgumentNullException(nameof(soapEnvelopeBuilder));
        _certificateProvider = certificateProvider;
        _rpsSignatureService = rpsSignatureService;
        _serviceTaxRateProvider = serviceTaxRateProvider ?? new Microled.Nfe.Service.Infra.Services.ServiceTaxRateProvider();
        _consultaNfeXsdValidator = consultaNfeXsdValidator;

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<RetornoEnvioLoteRpsResult> SendRpsBatchAsync(DomainEntities.RpsBatch batch, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SendRpsBatchAsync for batch with {Count} RPS", batch.RpsList.Count);

        try
        {
            // 1. Map domain entities to XSD-generated classes
            var pedido = MapRpsBatchToPedidoEnvioLoteRPS(batch);

            // 2. Serialize to XML (using specialized method for PedidoEnvioLoteRPS)
            var xmlContent = _xmlSerializer.SerializePedidoEnvioLoteRPS(pedido);
            LogXmlIfEnabled("Request XML (PedidoEnvioLoteRPS)", xmlContent);
            LogRpsFragmentsIfEnabled(xmlContent);
            _logger.LogDebug("Serialized PedidoEnvioLoteRPS XML (length: {Length})", xmlContent.Length);

            // 3. Build SOAP envelope (using specialized method for EnvioLoteRPS)
            var versaoSchema = int.Parse(_options.Versao.Replace(".", ""));
            var soapEnvelope = _soapEnvelopeBuilder.BuildEnvioLoteRPS(xmlContent, versaoSchema);

            // 4. Save XML to file before sending
            await SaveXmlToFileAsync(soapEnvelope, cancellationToken);

            // 5. Send HTTP request
            var endpoint = GetEndpoint();
            _logger.LogInformation("Sending SOAP request to {Endpoint}", endpoint);

            var requestContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            requestContent.Headers.Add("SOAPAction", $"{NfeNamespace}/ws/envioLoteRPS");

            var response = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);

            // 5. Read and parse SOAP response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            LogXmlIfEnabled("Response SOAP", responseContent);
            _logger.LogDebug("Received SOAP response (HTTP {StatusCode}, length: {Length})", 
                response.StatusCode, responseContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = BuildHttpErrorMessage("sending RPS batch", response.StatusCode, responseContent);
                _logger.LogError("HTTP error {StatusCode} when sending RPS batch. Details: {Details}", response.StatusCode, errorMessage);
                throw new NfeSoapException(
                    errorMessage,
                    (int)response.StatusCode);
            }


            // 6. Extract XML from SOAP response
            var retornoXml = ExtractXmlFromSoapResponse("EnvioLoteRPSResponse", responseContent);
         LogXmlIfEnabled("Response XML (RetornoEnvioLoteRPS)", retornoXml);

            // 7. Deserialize response
            var retorno = _xmlSerializer.Deserialize<RetornoEnvioLoteRPS>(retornoXml);

            // 8. Map to domain result
            var result = MapRetornoEnvioLoteRPSToResult(retorno, batch);

            // 9. Check for error 1206 and perform automatic signature comparison
            if (_rpsSignatureService != null)
            {
                CheckAndCompareSignatureErrors(result.Erros, batch);
            }

            _logger.LogInformation("SendRpsBatchAsync completed. Success: {Sucesso}, Protocolo: {Protocolo}",
                result.Sucesso, result.Protocolo);

            return result;
        }
        catch (NfeSoapException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendRpsBatchAsync");
            throw new NfeSoapException("Error sending RPS batch", ex);
        }
    }

    public async Task<ConsultaNfeResult> ConsultNfeAsync(ConsultNfeCriteria criteria, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ConsultNfeAsync for ChaveNFe: {ChaveNFe}, ChaveRps: {ChaveRps}",
            criteria.ChaveNFe, criteria.ChaveRps);

        try
        {
            // 1. Map domain criteria to XSD-generated classes
            var pedido = MapConsultNfeCriteriaToPedidoConsultaNFe(criteria);

            // 2. Serialize to XML
            var xmlContent = _xmlSerializer.SerializePedidoConsultaNFe(pedido);
            _consultaNfeXsdValidator?.Validate(xmlContent);
            LogXmlIfEnabled("Request XML (PedidoConsultaNFe)", xmlContent);
            _logger.LogDebug("Serialized PedidoConsultaNFe XML (length: {Length})", xmlContent.Length);

            // 3. Build SOAP envelope
            var versaoSchema = int.Parse(_options.Versao.Replace(".", ""));
            var soapEnvelope = _soapEnvelopeBuilder.BuildConsultaNFe(xmlContent, versaoSchema);
            LogXmlIfEnabled("Request SOAP (ConsultaNFe)", soapEnvelope);

            // 4. Send HTTP request
            var endpoint = GetEndpoint();
            _logger.LogInformation("Sending SOAP request to {Endpoint}", endpoint);

            var requestContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            requestContent.Headers.Add("SOAPAction", $"{NfeNamespace}/ws/consultaNFe");

            var response = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);

            // 5. Read and parse SOAP response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            LogXmlIfEnabled("Response SOAP", responseContent);
            _logger.LogDebug("Received SOAP response (HTTP {StatusCode}, length: {Length})",
                response.StatusCode, responseContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = BuildHttpErrorMessage("consulting NFe", response.StatusCode, responseContent);
                _logger.LogError("HTTP error {StatusCode} when consulting NFe. Details: {Details}", response.StatusCode, errorMessage);
                throw new NfeSoapException(
                    errorMessage,
                    (int)response.StatusCode);
            }

            // 6. Extract XML from SOAP response
            var retornoXml = ExtractXmlFromSoapResponse("ConsultaNFeResponse", responseContent);
            LogXmlIfEnabled("Response XML (RetornoConsulta)", retornoXml);

            // 7. Deserialize response
            var retorno = _xmlSerializer.Deserialize<RetornoConsulta>(retornoXml);

            // 8. Map to domain result
            var result = MapRetornoConsultaToResult(retorno);

            _logger.LogInformation("ConsultNfeAsync completed. Success: {Sucesso}, NFe count: {Count}",
                result.Sucesso, result.NFeList.Count);

            return result;
        }
        catch (NfeSoapException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ConsultNfeAsync");
            throw new NfeSoapException("Error consulting NFe", ex);
        }
    }

    public async Task<CancelNfeResult> CancelNfeAsync(DomainEntities.NfeCancellation cancellation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CancelNfeAsync for ChaveNFe: {ChaveNFe} with signature length {SignatureLength}",
            cancellation.ChaveNFe, cancellation.AssinaturaCancelamento?.Length ?? 0);

        try
        {
            // 1. Map domain cancellation to XSD-generated classes
            var pedido = MapNfeCancellationToPedidoCancelamentoNFe(cancellation);

            // 2. Serialize to XML
            var xmlContent = _xmlSerializer.Serialize(pedido);
            LogXmlIfEnabled("Request XML (PedidoCancelamentoNFe)", xmlContent);
            _logger.LogDebug("Serialized PedidoCancelamentoNFe XML (length: {Length})", xmlContent.Length);

            // 3. Build SOAP envelope
            var soapEnvelope = _soapEnvelopeBuilder.Build("CancelamentoNFe", xmlContent);

            // 4. Send HTTP request
            var endpoint = GetEndpoint();
            _logger.LogInformation("Sending SOAP request to {Endpoint}", endpoint);

            var requestContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            requestContent.Headers.Add("SOAPAction", $"{NfeNamespace}/CancelamentoNFe");

            var response = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);

            // 5. Read and parse SOAP response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            LogXmlIfEnabled("Response SOAP", responseContent);
            _logger.LogDebug("Received SOAP response (HTTP {StatusCode}, length: {Length})",
                response.StatusCode, responseContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = BuildHttpErrorMessage("canceling NFe", response.StatusCode, responseContent);
                _logger.LogError("HTTP error {StatusCode} when canceling NFe. Details: {Details}", response.StatusCode, errorMessage);
                throw new NfeSoapException(
                    errorMessage,
                    (int)response.StatusCode);
            }

            // 6. Extract XML from SOAP response
            var retornoXml = ExtractXmlFromSoapResponse("CancelamentoNFeResponse", responseContent);
            LogXmlIfEnabled("Response XML (RetornoCancelamentoNFe)", retornoXml);

            // 7. Deserialize response
            var retorno = _xmlSerializer.Deserialize<RetornoCancelamentoNFe>(retornoXml);

            // 8. Map to domain result
            var result = MapRetornoCancelamentoNFeToResult(retorno);

            _logger.LogInformation("CancelNfeAsync completed. Success: {Sucesso}",
                result.Sucesso);

            return result;
        }
        catch (NfeSoapException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CancelNfeAsync");
            throw new NfeSoapException("Error canceling NFe", ex);
        }
    }

    #region SOAP Envelope Methods

    /// <summary>
    /// Extracts XML content from SOAP response
    /// </summary>
    private string BuildHttpErrorMessage(string operationName, HttpStatusCode statusCode, string responseContent)
    {
        var baseMessage = $"HTTP error {statusCode} when {operationName}";

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return baseMessage;
        }

        try
        {
            var doc = XDocument.Parse(responseContent);
            var ns = XNamespace.Get(SoapNamespace);
            var fault = doc.Descendants(ns + "Fault").FirstOrDefault();

            if (fault != null)
            {
                var faultCode = fault.Element(ns + "faultcode")?.Value;
                var faultString = fault.Element(ns + "faultstring")?.Value;
                var faultDetail = fault.Element(ns + "detail")?.Value;

                var pieces = new[] { faultCode, faultString, faultDetail }
                    .Where(piece => !string.IsNullOrWhiteSpace(piece))
                    .ToArray();

                if (pieces.Length > 0)
                {
                    return $"{baseMessage}. SOAP Fault: {string.Join(" | ", pieces)}";
                }
            }
        }
        catch
        {
            // Ignore parsing failures and return a body preview instead.
        }

        var preview = responseContent.Length > 400
            ? responseContent[..400] + "..."
            : responseContent;

        return $"{baseMessage}. Response: {preview.Replace("\n", " ").Replace("\r", string.Empty)}";
    }

    private string ExtractXmlFromSoapResponse(string operationResponseName, string soapResponse)
    {
        try
        {
            // Parse SOAP response
            var doc = XDocument.Parse(soapResponse);
            var ns = XNamespace.Get(SoapNamespace);
            var nfeNs = XNamespace.Get(NfeNamespace);

            // Check for SOAP Fault
            var fault = doc.Descendants(ns + "Fault").FirstOrDefault();
            if (fault != null)
            {
                var faultCode = fault.Element(ns + "faultcode")?.Value;
                var faultString = fault.Element(ns + "faultstring")?.Value;
                var faultDetail = fault.Element(ns + "detail")?.ToString();

                _logger.LogError("SOAP Fault received. Code: {FaultCode}, String: {FaultString}",
                    faultCode, faultString);

                throw new NfeSoapException(
                    $"SOAP Fault: {faultString}",
                    faultCode,
                    faultString,
                    faultDetail);
            }

            // Extract RetornoXML from response
            // CORREÇÃO: A prefeitura usa "RetornoXML" em vez de "MensagemXML" para EnvioLoteRPS
            var responseElement = doc.Descendants(nfeNs + operationResponseName).FirstOrDefault();
            if (responseElement == null)
            {
                throw new NfeSoapException($"Could not find {operationResponseName} element in SOAP response");
            }

            // Try RetornoXML first (used by EnvioLoteRPS), then fallback to MensagemXML (used by other operations)
            var retornoXmlElement = responseElement.Element(nfeNs + "RetornoXML");
            var mensagemXmlElement = retornoXmlElement ?? responseElement.Element(nfeNs + "MensagemXML");
            
            if (mensagemXmlElement == null)
            {
                _logger.LogError("Could not find RetornoXML or MensagemXML element in SOAP response. Available elements: {Elements}",
                    string.Join(", ", responseElement.Elements().Select(e => e.Name.LocalName)));
                throw new NfeSoapException("Could not find RetornoXML or MensagemXML element in SOAP response");
            }

            // Handle CDATA content
            var xmlContent = mensagemXmlElement.Value;
            if (string.IsNullOrEmpty(xmlContent) && mensagemXmlElement.Nodes().Any())
            {
                // Try to get CDATA content
                var cdata = mensagemXmlElement.Nodes().OfType<XCData>().FirstOrDefault();
                if (cdata != null)
                {
                    xmlContent = cdata.Value;
                }
            }

            if (string.IsNullOrEmpty(xmlContent))
            {
                throw new NfeSoapException("RetornoXML/MensagemXML element is empty in SOAP response");
            }

            return xmlContent;
        }
        catch (NfeSoapException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting XML from SOAP response");
            throw new NfeSoapException("Error parsing SOAP response", ex);
        }
    }

    /// <summary>
    /// Gets the endpoint URL based on configuration
    /// </summary>
    private string GetEndpoint()
    {
        // Use BaseUrl if configured, otherwise fallback to ProductionEndpoint/TestEndpoint
        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            return _options.BaseUrl;
        }

        var endpoint = _options.UseProduction ? _options.ProductionEndpoint : _options.TestEndpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException(
                $"NFe service endpoint is not configured. Please set either {nameof(NfeServiceOptions.BaseUrl)} " +
                $"or {(_options.UseProduction ? nameof(NfeServiceOptions.ProductionEndpoint) : nameof(NfeServiceOptions.TestEndpoint))} " +
                "in appsettings.json");
        }

        return endpoint;
    }

    /// <summary>
    /// Saves XML content to file in C:\XML_SEND directory
    /// </summary>
    private async Task SaveXmlToFileAsync(string xmlContent, CancellationToken cancellationToken)
    {
        try
        {
            const string outputDirectory = @"C:\XML_SEND";
            
            // Ensure directory exists
            Directory.CreateDirectory(outputDirectory);

            // Generate file name with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"EnvioLoteRPS_{timestamp}_{guid}.xml";
            var filePath = Path.Combine(outputDirectory, fileName);

            // Save XML to file
            await File.WriteAllTextAsync(filePath, xmlContent, Encoding.UTF8, cancellationToken);
            
            _logger.LogInformation("Saved XML to file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            _logger.LogWarning(ex, "Failed to save XML to file");
        }
    }

    /// <summary>
    /// Logs XML content if LogRawXml is enabled, with optional masking of sensitive data
    /// </summary>
    private void LogXmlIfEnabled(string label, string xml)
    {
        if (!_options.LogRawXml)
            return;

        if (_options.LogSensitiveData)
        {
            _logger.LogTrace("{Label}: {Xml}", label, xml);
        }
        else
        {
            var maskedXml = MaskSensitiveData(xml);
            _logger.LogTrace("{Label}: {Xml}", label, maskedXml);
        }
    }

    /// <summary>
    /// Logs only the &lt;RPS&gt;...&lt;/RPS&gt; fragments (useful for schema troubleshooting) when LogRawXml is enabled.
    /// Respects LogSensitiveData masking rules.
    /// </summary>
    private void LogRpsFragmentsIfEnabled(string pedidoEnvioLoteRpsXml)
    {
        if (!_options.LogRawXml || string.IsNullOrWhiteSpace(pedidoEnvioLoteRpsXml))
            return;

        var matches = Regex.Matches(pedidoEnvioLoteRpsXml, @"<RPS\b[\s\S]*?</RPS>", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
            return;

        foreach (Match m in matches)
        {
            var fragment = m.Value;
            LogXmlIfEnabled("RPS fragment", fragment);
        }
    }

    /// <summary>
    /// Masks sensitive data in XML (CNPJ, CPF, monetary values, keys)
    /// </summary>
    private string MaskSensitiveData(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return xml;

        var masked = xml;

        // Mask CNPJ (14 digits): keep first 4 and last 4
        masked = Regex.Replace(masked, @"(\d{4})(\d{6})(\d{4})", m => $"{m.Groups[1].Value}******{m.Groups[3].Value}");

        // Mask CPF (11 digits): keep first 3 and last 2
        masked = Regex.Replace(masked, @"(\d{3})(\d{6})(\d{2})", m => $"{m.Groups[1].Value}******{m.Groups[3].Value}");

        // Mask monetary values (decimal numbers with 2 decimal places)
        masked = Regex.Replace(masked, @"(\d+\.\d{2})", "***.**");

        // Mask Base64 signatures (long base64 strings)
        masked = Regex.Replace(masked, @"([A-Za-z0-9+/]{50,}={0,2})", "***BASE64_SIGNATURE***");

        return masked;
    }

    #endregion

    #region Mapping Methods - Domain to XML

    private PedidoEnvioLoteRPS MapRpsBatchToPedidoEnvioLoteRPS(DomainEntities.RpsBatch batch)
    {
        if (batch.RpsList.Count == 0)
            throw new ArgumentException("RPS batch cannot be empty", nameof(batch));

        var primeiroRps = batch.RpsList[0];
        var prestador = primeiroRps.Prestador;

        // CPFCNPJRemetente deve ser o CNPJ do certificado digital usado na autenticação
        // Se não houver certificado, usar o CNPJ do prestador como fallback
        tpCPFCNPJ cpfCnpjRemetente;
        string? cnpjFromCertificate = null;
        
        if (_certificateProvider != null)
        {
            try
            {
                var certificate = _certificateProvider.GetCertificate();
                cnpjFromCertificate = ExtractCnpjFromCertificate(certificate);
                if (!string.IsNullOrEmpty(cnpjFromCertificate))
                {
                    cpfCnpjRemetente = new tpCPFCNPJ { CNPJ = cnpjFromCertificate };
                    _logger.LogInformation("Using CNPJ from certificate for CPFCNPJRemetente: {CNPJ}", cnpjFromCertificate);
                    
                    // Validação: alertar se o CNPJ do certificado difere do CNPJ do prestador
                    var prestadorCnpj = prestador.CpfCnpj.GetValue();
                    if (!string.Equals(cnpjFromCertificate, prestadorCnpj, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "CNPJ mismatch detected: Certificate CNPJ ({CertificateCNPJ}) differs from Prestador CNPJ ({PrestadorCNPJ}). " +
                            "The InscricaoMunicipal ({IM}) must correspond to the Certificate CNPJ. " +
                            "If you get error 1202 (Prestador não encontrado), verify that the IM is registered for the Certificate CNPJ.",
                            cnpjFromCertificate, prestadorCnpj, primeiroRps.ChaveRPS.InscricaoPrestador);
                    }
                }
                else
                {
                    // Fallback: usar CNPJ do prestador se não conseguir extrair do certificado
                    cpfCnpjRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj);
                    _logger.LogWarning("Could not extract CNPJ from certificate, using prestador CNPJ: {CNPJ}", prestador.CpfCnpj.GetValue());
                }
            }
            catch (Exception ex)
            {
                // Fallback: usar CNPJ do prestador se houver erro ao obter certificado
                _logger.LogWarning(ex, "Error getting certificate, using prestador CNPJ for CPFCNPJRemetente");
                cpfCnpjRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj);
            }
        }
        else
        {
            // Sem certificado provider, usar CNPJ do prestador
            cpfCnpjRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj);
        }

        var pedido = new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = long.Parse(_options.Versao.Replace(".", "")),
                CPFCNPJRemetente = cpfCnpjRemetente,
                transacao = batch.Transacao,
                dtInicio = batch.DataInicio.ToDateTime(TimeOnly.MinValue),
                dtFim = batch.DataFim.ToDateTime(TimeOnly.MinValue),
                QtdRPS = batch.RpsList.Count
            },
            RPS = batch.RpsList.Select(MapRpsToTpRPS).ToList()
        };

        return pedido;
    }

    private tpRPS MapRpsToTpRPS(DomainEntities.Rps rps)
    {
        if (string.IsNullOrEmpty(rps.Assinatura))
            throw new InvalidOperationException($"RPS {rps.ChaveRPS.NumeroRps} does not have a signature. It must be signed before sending.");

        // Convert Base64 signature to byte array
        var assinaturaBytes = Convert.FromBase64String(rps.Assinatura);

        // Layout 2 / IBSCBS: cClassTrib obrigatório (erro 628 quando inexistente/não vigente)
        var versaoSchema = int.Parse(_options.Versao.Replace(".", ""));
        string cClassTrib;
        if (versaoSchema >= 2)
        {
            // Compatibilidade: alguns fluxos (ex. API) podem não informar cClassTrib.
            // Nesses casos, usamos fallback "000001" (tabela vigente) para evitar erro 628.
            var cClassTribRaw = rps.IbsCbsCClassTrib;
            cClassTrib = IbsCbsCClassTribValidator.ValidateAndGet(cClassTribRaw ?? "000001");

            if (string.IsNullOrWhiteSpace(cClassTribRaw))
            {
                _logger.LogWarning(
                    "IBSCBS_CClassTrib ausente; usando fallback {Fallback}. RPS {InscricaoPrestador}-{NumeroRps}, CodigoServico={CodigoServico}",
                    cClassTrib,
                    rps.ChaveRPS.InscricaoPrestador,
                    rps.ChaveRPS.NumeroRps,
                    rps.Item.CodigoServico);
            }
            else if (!string.Equals(cClassTribRaw, cClassTrib, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "IBSCBS_CClassTrib normalizado. Raw='{Raw}' => Final='{Final}'. RPS {InscricaoPrestador}-{NumeroRps}, CodigoServico={CodigoServico}",
                    cClassTribRaw,
                    cClassTrib,
                    rps.ChaveRPS.InscricaoPrestador,
                    rps.ChaveRPS.NumeroRps,
                    rps.Item.CodigoServico);
            }
        }
        else
        {
            cClassTrib = "0";
        }

        // Layout 2 / IBSCBS: cIndOp deve ser sempre 6 dígitos numéricos; PMSP exige 100301 para nosso caso.
        // Mesmo que venha do Access, normalizamos e fazemos fallback para 100301 quando vazio/nulo/inválido.
        var cIndOp = IbsCbsCIndOpNormalizer.NormalizeOrDefault(rps.IbsCbsCIndOp);

        var originalAliquota = rps.Item.AliquotaServicos.Value;
        var providerAliquota = _serviceTaxRateProvider.GetAliquota(rps.Item.CodigoServico);
        var aliquotaToSend = providerAliquota != 0m ? providerAliquota : originalAliquota;

        if (aliquotaToSend != originalAliquota)
        {
            _logger.LogInformation(
                "AliquotaServicos adjusted from {From} to {To} for CodigoServico {CodigoServico}",
                originalAliquota,
                aliquotaToSend,
                rps.Item.CodigoServico);
        }

        // Regra erro 1630: CodigoServico 2919 não permite informar ValorTotalRecebido => omitir a tag (null)
        var omitValorTotalRecebido = rps.Item.CodigoServico == 2919;
        var valorTotalRecebido = omitValorTotalRecebido ? (decimal?)null : rps.Item.ValorServicos.Value;

        _logger.LogInformation(
            "RPS XML fields: CodigoServico={CodigoServico}, AliquotaServicos={AliquotaServicos}, OmitValorTotalRecebido={OmitValorTotalRecebido}",
            rps.Item.CodigoServico,
            aliquotaToSend,
            omitValorTotalRecebido);

        if (versaoSchema >= 2)
        {
            _logger.LogInformation(
                "Applying IBSCBS cClassTrib={cClassTrib}, cIndOp={cIndOp} for RPS {InscricaoPrestador}-{NumeroRps}",
                cClassTrib,
                cIndOp,
                rps.ChaveRPS.InscricaoPrestador,
                rps.ChaveRPS.NumeroRps);
        }

        var tpRps = new tpRPS
        {
            Assinatura = assinaturaBytes,
            ChaveRPS = new tpChaveRPS
            {
                InscricaoPrestador = rps.ChaveRPS.InscricaoPrestador,
                SerieRPS = rps.ChaveRPS.SerieRps,
                NumeroRPS = rps.ChaveRPS.NumeroRps
            },
            TipoRPS = MapTipoRpsToString(rps.TipoRPS),
            DataEmissao = rps.DataEmissao.ToDateTime(TimeOnly.MinValue),
            StatusRPS = ((char)rps.StatusRPS).ToString(),
            TributacaoRPS = ((char)rps.TributacaoRPS).ToString(),
            ValorDeducoes = rps.Item.ValorDeducoes.Value,
            ValorPIS = 0.00m,
            ValorCOFINS = 0.00m,
            ValorINSS = 0.00m,
            ValorIR = 0.00m,
            ValorCSLL = 0.00m,
            CodigoServico = rps.Item.CodigoServico,
            AliquotaServicos = aliquotaToSend,
            ISSRetido = rps.Item.IssRetido == IssRetido.Sim,
            Discriminacao = rps.Item.Discriminacao,
            // IMPORTANTE:
            // O schema v2 do RPS da prefeitura usa campos como ValorTotalRecebido / ValorInicialCobrado / ValorFinalCobrado.
            // Se não preencher, o webservice tende a assumir 0 (e então a "String verificada" do 1206 vem com ValorServicos=0).
            // Para manter consistência entre XML e assinatura, espelhamos o ValorServicos do domínio nesses campos.
            ValorTotalRecebido = valorTotalRecebido,
            ValorInicialCobrado = rps.Item.ValorServicos.Value,
            ValorFinalCobrado = rps.Item.ValorServicos.Value,
            ValorIPI = 0.00m,
            ExigibilidadeSuspensa = 0,
            PagamentoParceladoAntecipado = 0,
            NBS = "123456789", // TODO: Get from configuration or RPS item
            // Como nosso serviço é prestado no Brasil, preencher cLocPrestacao por padrão
            // Usar código do município do prestador se disponível, senão usar São Paulo (3550308)
            cLocPrestacao = rps.Prestador.Endereco?.CodigoMunicipio ?? 3550308, // São Paulo por padrão
            cPaisPrestacao = null, // NUNCA preencher (sistema não suporta prestação fora do Brasil)
            IBSCBS = CreateDefaultIBSCBS(cClassTrib, cIndOp)
        };

        // Fail-fast:
        // - Decisão de negócio: ignorar cPaisPrestacao (não suportamos prestação fora do Brasil)
        // - Manter cLocPrestacao para Brasil (schema exige gpPrestacao antes do IBSCBS)
        var tributacaoIsT = string.Equals(tpRps.TributacaoRPS, "T", StringComparison.OrdinalIgnoreCase);

        if (tributacaoIsT)
        {
            if (tpRps.MunicipioPrestacao.HasValue)
            {
                _logger.LogWarning(
                    "Removido MunicipioPrestacao por TributacaoRPS=T (regra erro 1223). MunicipioPrestacao={MunicipioPrestacao}",
                    tpRps.MunicipioPrestacao);
            }

            tpRps.MunicipioPrestacao = null;
        }

        if (!string.IsNullOrWhiteSpace(tpRps.cPaisPrestacao))
        {
            _logger.LogInformation("Ignoring cPaisPrestacao — system does not support services outside Brazil. Provided='{Provided}'", tpRps.cPaisPrestacao);
            tpRps.cPaisPrestacao = null;
        }

        // Map tomador if present
        if (rps.Tomador != null)
        {
            tpRps.CPFCNPJTomador = MapCpfCnpjToTpCPFCNPJNIF(rps.Tomador.CpfCnpj);
            tpRps.InscricaoMunicipalTomador = rps.Tomador.InscricaoMunicipal;
            tpRps.InscricaoEstadualTomador = rps.Tomador.InscricaoEstadual;
            tpRps.RazaoSocialTomador = rps.Tomador.RazaoSocial;
            tpRps.EnderecoTomador = MapAddressToTpEndereco(rps.Tomador.Endereco);
            tpRps.EmailTomador = rps.Tomador.Email;
        }

        return tpRps;
    }

    private PedidoConsultaNFe MapConsultNfeCriteriaToPedidoConsultaNFe(ConsultNfeCriteria criteria)
    {
        if (criteria.ChaveNFe == null && criteria.ChaveRps == null)
            throw new ArgumentException("Either ChaveNFe or ChaveRps must be provided", nameof(criteria));

        // Get CNPJ from options or use a default (should be configured)
        var cnpjRemetente = _options.DefaultCnpjRemetente ?? throw new InvalidOperationException(
            "DefaultCnpjRemetente must be configured in NfeServiceOptions");

        var pedido = new PedidoConsultaNFe
        {
            Cabecalho = new PedidoConsultaNFeCabecalho
            {
                Versao = long.Parse(_options.Versao.Replace(".", "")),
                CPFCNPJRemetente = new tpCPFCNPJ { CNPJ = cnpjRemetente }
            },
            Detalhe = new List<PedidoConsultaNFeDetalhe>()
        };

        var detalhe = new PedidoConsultaNFeDetalhe();
        if (criteria.ChaveNFe != null)
        {
            detalhe.ChaveNFe = new tpChaveNFe
            {
                InscricaoPrestador = criteria.ChaveNFe.InscricaoPrestador,
                NumeroNFe = criteria.ChaveNFe.NumeroNFe,
                CodigoVerificacao = criteria.ChaveNFe.CodigoVerificacao,
                ChaveNotaNacional = criteria.ChaveNFe.ChaveNotaNacional
            };
        }
        else if (criteria.ChaveRps != null)
        {
            detalhe.ChaveRPS = new tpChaveRPS
            {
                InscricaoPrestador = criteria.ChaveRps.InscricaoPrestador,
                SerieRPS = criteria.ChaveRps.SerieRps,
                NumeroRPS = criteria.ChaveRps.NumeroRps
            };
        }

        pedido.Detalhe.Add(detalhe);

        return pedido;
    }

    private PedidoCancelamentoNFe MapNfeCancellationToPedidoCancelamentoNFe(DomainEntities.NfeCancellation cancellation)
    {
        if (string.IsNullOrEmpty(cancellation.AssinaturaCancelamento))
            throw new InvalidOperationException("Cancellation signature is required");

        // Get CNPJ from options or use a default (should be configured)
        var cnpjRemetente = _options.DefaultCnpjRemetente ?? throw new InvalidOperationException(
            "DefaultCnpjRemetente must be configured in NfeServiceOptions");

        // Convert Base64 signature to byte array
        var assinaturaBytes = Convert.FromBase64String(cancellation.AssinaturaCancelamento);

        var pedido = new PedidoCancelamentoNFe
        {
            Cabecalho = new PedidoCancelamentoNFeCabecalho
            {
                Versao = long.Parse(_options.Versao.Replace(".", "")),
                CPFCNPJRemetente = new tpCPFCNPJ { CNPJ = cnpjRemetente },
                transacao = true
            },
            Detalhe = new List<PedidoCancelamentoNFeDetalhe>
            {
                new PedidoCancelamentoNFeDetalhe
                {
                    ChaveNFe = new tpChaveNFe
                    {
                        InscricaoPrestador = cancellation.ChaveNFe.InscricaoPrestador,
                        NumeroNFe = cancellation.ChaveNFe.NumeroNFe,
                        CodigoVerificacao = cancellation.ChaveNFe.CodigoVerificacao,
                        ChaveNotaNacional = cancellation.ChaveNFe.ChaveNotaNacional
                    },
                    AssinaturaCancelamento = assinaturaBytes
                }
            }
        };

        return pedido;
    }

    #endregion

    #region Mapping Methods - XML to Domain

    private RetornoEnvioLoteRpsResult MapRetornoEnvioLoteRPSToResult(RetornoEnvioLoteRPS retorno, DomainEntities.RpsBatch? batch = null)
    {
        if (retorno == null)
            throw new ArgumentNullException(nameof(retorno));

        if (retorno.Cabecalho == null)
            throw new InvalidOperationException("RetornoEnvioLoteRPS.Cabecalho is null");

        var result = new RetornoEnvioLoteRpsResult
        {
            Sucesso = retorno.Cabecalho.Sucesso,
            Alertas = (retorno.Alerta ?? new List<tpEvento>()).Select(MapTpEventoToEvento).ToList(),
            Erros = (retorno.Erro ?? new List<tpEvento>()).Select(MapTpEventoToEvento).ToList(),
            ChavesNFeRPS = (retorno.ChaveNFeRPS ?? new List<tpChaveNFeRPS>()).Select(MapTpChaveNFeRPSToNfeRpsKeyPair).ToList()
        };

        // Extract protocolo from InformacoesLote if available
        if (retorno.Cabecalho.InformacoesLote?.NumeroLote != null)
        {
            result.Protocolo = retorno.Cabecalho.InformacoesLote.NumeroLote.ToString();
        }

        return result;
    }

    private ConsultaNfeResult MapRetornoConsultaToResult(RetornoConsulta retorno)
    {
        var result = new ConsultaNfeResult
        {
            Sucesso = retorno.Cabecalho.Sucesso,
            Alertas = retorno.Alerta.Select(MapTpEventoToEvento).ToList(),
            Erros = retorno.Erro.Select(MapTpEventoToEvento).ToList(),
            NFeList = retorno.NFe.Select(MapTpNFeToNfe).ToList()
        };

        return result;
    }

    private CancelNfeResult MapRetornoCancelamentoNFeToResult(RetornoCancelamentoNFe retorno)
    {
        var result = new CancelNfeResult
        {
            Sucesso = retorno.Cabecalho.Sucesso,
            Alertas = retorno.Alerta.Select(MapTpEventoToEvento).ToList(),
            Erros = retorno.Erro.Select(MapTpEventoToEvento).ToList()
        };

        return result;
    }

    #endregion

    #region Helper Mapping Methods

    private tpCPFCNPJ MapCpfCnpjToTpCPFCNPJ(CpfCnpj cpfCnpj)
    {
        if (cpfCnpj == null)
            throw new ArgumentNullException(nameof(cpfCnpj));

        var value = cpfCnpj.GetValue();
        if (cpfCnpj.IsCpf)
        {
            return new tpCPFCNPJ { CPF = value };
        }
        else
        {
            return new tpCPFCNPJ { CNPJ = value };
        }
    }

    private tpCPFCNPJNIF? MapCpfCnpjToTpCPFCNPJNIF(CpfCnpj? cpfCnpj)
    {
        if (cpfCnpj == null)
            return null;

        var value = cpfCnpj.GetValue();
        if (cpfCnpj.IsCpf)
        {
            return new tpCPFCNPJNIF { CPF = value };
        }
        else
        {
            return new tpCPFCNPJNIF { CNPJ = value };
        }
    }

    private tpEndereco? MapAddressToTpEndereco(Address? address)
    {
        if (address == null)
            return null;

        return new tpEndereco
        {
            TipoLogradouro = address.TipoLogradouro,
            Logradouro = address.Logradouro,
            NumeroEndereco = address.Numero,
            ComplementoEndereco = address.Complemento,
            Bairro = address.Bairro,
            Cidade = address.CodigoMunicipio,
            UF = address.UF,
            CEP = address.CEP
        };
    }

    private string MapTipoRpsToString(TipoRps tipoRps)
    {
        return tipoRps switch
        {
            TipoRps.RPS => "RPS",
            TipoRps.RPS_M => "RPS-M",
            TipoRps.RPS_C => "RPS-C",
            _ => throw new ArgumentException($"Unknown TipoRps: {tipoRps}")
        };
    }

    private Evento MapTpEventoToEvento(tpEvento tpEvento)
    {
        return new Evento
        {
            Codigo = tpEvento.Codigo,
            Descricao = tpEvento.Descricao,
            ChaveRPS = tpEvento.ChaveRPS != null ? MapTpChaveRPSToRpsKey(tpEvento.ChaveRPS) : null,
            ChaveNFe = tpEvento.ChaveNFe != null ? MapTpChaveNFeToNfeKey(tpEvento.ChaveNFe) : null
        };
    }

    /// <summary>
    /// Checks for error 1206 (signature error) and performs automatic comparison
    /// </summary>
    private void CheckAndCompareSignatureErrors(List<Evento> erros, DomainEntities.RpsBatch batch)
    {
        const int ErrorCode1206 = 1206;
        
        var signatureErrors = erros.Where(e => e.Codigo == ErrorCode1206).ToList();
        if (!signatureErrors.Any())
            return;

        _logger.LogError("Error 1206 detected: {Count} signature error(s) found", signatureErrors.Count);

        foreach (var erro in signatureErrors)
        {
            if (erro.ChaveRPS == null)
            {
                _logger.LogWarning("Error 1206 found but no ChaveRPS provided, cannot compare signature");
                continue;
            }

            // Find the corresponding RPS in the batch
            var rps = batch.RpsList.FirstOrDefault(r => 
                r.ChaveRPS.InscricaoPrestador == erro.ChaveRPS.InscricaoPrestador &&
                r.ChaveRPS.NumeroRps == erro.ChaveRPS.NumeroRps &&
                r.ChaveRPS.SerieRps == erro.ChaveRPS.SerieRps);

            if (rps == null)
            {
                _logger.LogWarning(
                    "Error 1206 for RPS {InscricaoPrestador}-{NumeroRps} but RPS not found in batch",
                    erro.ChaveRPS.InscricaoPrestador,
                    erro.ChaveRPS.NumeroRps);
                continue;
            }

            // Extract signature string from error message
            _logger.LogInformation("Extracting verified signature string (1206) for RPS {InscricaoPrestador}-{NumeroRps}", 
                erro.ChaveRPS.InscricaoPrestador, erro.ChaveRPS.NumeroRps);
            _logger.LogDebug("1206 description: {Descricao}", erro.Descricao);
            
            if (string.IsNullOrWhiteSpace(erro.Descricao))
            {
                _logger.LogError(
                    "❌ Error 1206 for RPS {InscricaoPrestador}-{NumeroRps} but error description is empty",
                    erro.ChaveRPS.InscricaoPrestador,
                    erro.ChaveRPS.NumeroRps);
                continue;
            }
            
            var prefeituraString = _rpsSignatureService!.ExtractSignatureStringFromError(erro.Descricao);
            if (prefeituraString == null)
            {
                _logger.LogError(
                    "❌ Error 1206 for RPS {InscricaoPrestador}-{NumeroRps} but could not extract signature string from error message: {Descricao}",
                    erro.ChaveRPS.InscricaoPrestador,
                    erro.ChaveRPS.NumeroRps,
                    erro.Descricao);
                continue;
            }

            _logger.LogInformation("Verified string extracted. Length={Length}", 
                prefeituraString.Length);
            _logger.LogDebug("Verified string: {String}", 
                prefeituraString.Replace(' ', '·'));

            // Build our signature string
            _logger.LogInformation("Building our signature string for RPS {InscricaoPrestador}-{NumeroRps}", 
                erro.ChaveRPS.InscricaoPrestador, erro.ChaveRPS.NumeroRps);
            var ourString = _rpsSignatureService.BuildSignatureString(rps);
            
            _logger.LogInformation("Our string built. Length={Length}", ourString.Length);
            _logger.LogDebug("Our string: {String}", ourString.Replace(' ', '·'));

            // Compare strings first
            _logger.LogInformation("Comparing signature strings (char-by-char)...");
            var stringsMatch = _rpsSignatureService.CompareSignatureStrings(ourString, prefeituraString, rps);

            // If strings don't match, try auto-fix
            if (!stringsMatch)
            {
                _logger.LogWarning("Attempting auto-fix for RPS {InscricaoPrestador}-{NumeroRps}...", 
                    erro.ChaveRPS.InscricaoPrestador, erro.ChaveRPS.NumeroRps);
                
                var fixedString = _rpsSignatureService.AutoFixSignatureString(rps, prefeituraString);
                if (fixedString != null)
                {
                    _logger.LogWarning(
                        "Auto-fix found a matching string for RPS {InscricaoPrestador}-{NumeroRps}. " +
                        "Consider updating the fixed rules used in BuildSignatureString.",
                        erro.ChaveRPS.InscricaoPrestador,
                        erro.ChaveRPS.NumeroRps
                    );
                }
                else
                {
                    _logger.LogError(
                        "Auto-fix could not find a matching strategy for RPS {InscricaoPrestador}-{NumeroRps}. " +
                        "Review the mismatch log (first diff index/char) and adjust signature rules.",
                        erro.ChaveRPS.InscricaoPrestador,
                        erro.ChaveRPS.NumeroRps
                    );
                }
            }
        }
    }

    private DomainEntities.RpsKey MapTpChaveRPSToRpsKey(tpChaveRPS tpChaveRPS)
    {
        return new DomainEntities.RpsKey(
            tpChaveRPS.InscricaoPrestador,
            tpChaveRPS.NumeroRPS,
            tpChaveRPS.SerieRPS);
    }

    private DomainEntities.NfeKey MapTpChaveNFeToNfeKey(tpChaveNFe tpChaveNFe)
    {
        return new DomainEntities.NfeKey(
            tpChaveNFe.InscricaoPrestador,
            tpChaveNFe.NumeroNFe,
            tpChaveNFe.CodigoVerificacao,
            tpChaveNFe.ChaveNotaNacional);
    }

    private NfeRpsKeyPair MapTpChaveNFeRPSToNfeRpsKeyPair(tpChaveNFeRPS tpChaveNFeRPS)
    {
        return new NfeRpsKeyPair
        {
            ChaveNFe = MapTpChaveNFeToNfeKey(tpChaveNFeRPS.ChaveNFe),
            ChaveRPS = MapTpChaveRPSToRpsKey(tpChaveNFeRPS.ChaveRPS)
        };
    }

    private DomainEntities.Nfe MapTpNFeToNfe(tpNFe tpNFe)
    {
        return new DomainEntities.Nfe(
            MapTpChaveNFeToNfeKey(tpNFe.ChaveNFe),
            tpNFe.DataEmissaoNFe,
            tpNFe.DataFatoGeradorNFe,
            tpNFe.StatusNFe,
            Money.Create(tpNFe.ValorServicos),
            Money.Create(tpNFe.ValorDeducoes ?? 0),
            Money.Create(tpNFe.ValorISS),
            tpNFe.ChaveNFe.CodigoVerificacao);
    }

    private tpIBSCBS CreateDefaultIBSCBS(string cClassTrib, string cIndOp)
    {
        // Create a default IBSCBS structure
        // TODO: This should be configurable or come from RPS item
        return new tpIBSCBS
        {
            finNFSe = 0, // NFS-e regular
            indFinal = 0, // Não é consumidor final
            cIndOp = cIndOp,
            indDest = 0, // Não informado
            valores = new tpValores
            {
                trib = new tpTrib
                {
                    gIBSCBS = new tpGIBSCBS
                    {
                        cClassTrib = cClassTrib
                    }
                }
            }
        };
    }

    /// <summary>
    /// Extrai o CNPJ do certificado digital a partir do Subject.
    /// O CNPJ geralmente está no formato "CN=RAZAO SOCIAL:47208271000109" ou similar.
    /// </summary>
    private static string? ExtractCnpjFromCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        if (certificate == null)
            return null;

        var subject = certificate.Subject;
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        // Tenta encontrar CNPJ no Subject (formato comum: "CN=RAZAO SOCIAL:47208271000109")
        // O CNPJ pode estar após dois pontos (:) ou no final do Subject
        var parts = subject.Split(':', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            // CNPJ tem 14 dígitos
            if (trimmed.Length >= 14)
            {
                // Tenta extrair sequência de 14 dígitos
                var digits = new System.Text.StringBuilder();
                foreach (var c in trimmed)
                {
                    if (char.IsDigit(c))
                        digits.Append(c);
                }
                
                if (digits.Length == 14)
                {
                    return digits.ToString();
                }
            }
        }

        // Tenta encontrar CNPJ em qualquer parte do Subject usando regex
        var cnpjPattern = @"(\d{14})";
        var match = Regex.Match(subject, cnpjPattern);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    #endregion
}
