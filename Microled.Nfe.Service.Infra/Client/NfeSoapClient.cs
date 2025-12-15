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

    public NfeSoapClient(
        HttpClient httpClient,
        ILogger<NfeSoapClient> logger,
        IOptions<NfeServiceOptions> options,
        IXmlSerializerService xmlSerializer,
        ISoapEnvelopeBuilder soapEnvelopeBuilder)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value;
        _xmlSerializer = xmlSerializer ?? throw new ArgumentNullException(nameof(xmlSerializer));
        _soapEnvelopeBuilder = soapEnvelopeBuilder ?? throw new ArgumentNullException(nameof(soapEnvelopeBuilder));

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<RetornoEnvioLoteRpsResult> SendRpsBatchAsync(DomainEntities.RpsBatch batch, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SendRpsBatchAsync for batch with {Count} RPS", batch.RpsList.Count);

        try
        {
            // 1. Map domain entities to XSD-generated classes
            var pedido = MapRpsBatchToPedidoEnvioLoteRPS(batch);

            // 2. Serialize to XML
            var xmlContent = _xmlSerializer.Serialize(pedido);
            LogXmlIfEnabled("Request XML (PedidoEnvioLoteRPS)", xmlContent);
            _logger.LogDebug("Serialized PedidoEnvioLoteRPS XML (length: {Length})", xmlContent.Length);

            // 3. Build SOAP envelope
            var soapEnvelope = _soapEnvelopeBuilder.Build("EnvioLoteRPS", xmlContent);

            // 4. Send HTTP request
            var endpoint = GetEndpoint();
            _logger.LogInformation("Sending SOAP request to {Endpoint}", endpoint);

            var requestContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            requestContent.Headers.Add("SOAPAction", $"{NfeNamespace}/EnvioLoteRPS");

            var response = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);

            // 5. Read and parse SOAP response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            LogXmlIfEnabled("Response SOAP", responseContent);
            _logger.LogDebug("Received SOAP response (HTTP {StatusCode}, length: {Length})", 
                response.StatusCode, responseContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HTTP error {StatusCode} when sending RPS batch", response.StatusCode);
                throw new NfeSoapException(
                    $"HTTP error {response.StatusCode} when sending RPS batch",
                    (int)response.StatusCode);
            }

            // 6. Extract XML from SOAP response
            var retornoXml = ExtractXmlFromSoapResponse("EnvioLoteRPSResponse", responseContent);
            LogXmlIfEnabled("Response XML (RetornoEnvioLoteRPS)", retornoXml);

            // 7. Deserialize response
            var retorno = _xmlSerializer.Deserialize<RetornoEnvioLoteRPS>(retornoXml);

            // 8. Map to domain result
            var result = MapRetornoEnvioLoteRPSToResult(retorno);

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
            var xmlContent = _xmlSerializer.Serialize(pedido);
            LogXmlIfEnabled("Request XML (PedidoConsultaNFe)", xmlContent);
            _logger.LogDebug("Serialized PedidoConsultaNFe XML (length: {Length})", xmlContent.Length);

            // 3. Build SOAP envelope
            var soapEnvelope = _soapEnvelopeBuilder.Build("ConsultaNFe", xmlContent);

            // 4. Send HTTP request
            var endpoint = GetEndpoint();
            _logger.LogInformation("Sending SOAP request to {Endpoint}", endpoint);

            var requestContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            requestContent.Headers.Add("SOAPAction", $"{NfeNamespace}/ConsultaNFe");

            var response = await _httpClient.PostAsync(endpoint, requestContent, cancellationToken);

            // 5. Read and parse SOAP response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received SOAP response (HTTP {StatusCode}, length: {Length})",
                response.StatusCode, responseContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HTTP error {StatusCode} when consulting NFe", response.StatusCode);
                throw new NfeSoapException(
                    $"HTTP error {response.StatusCode} when consulting NFe",
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
                _logger.LogError("HTTP error {StatusCode} when canceling NFe", response.StatusCode);
                throw new NfeSoapException(
                    $"HTTP error {response.StatusCode} when canceling NFe",
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

            // Extract MensagemXML from response
            var responseElement = doc.Descendants(nfeNs + operationResponseName).FirstOrDefault();
            if (responseElement == null)
            {
                throw new NfeSoapException($"Could not find {operationResponseName} element in SOAP response");
            }

            var mensagemXmlElement = responseElement.Element(nfeNs + "MensagemXML");
            if (mensagemXmlElement == null)
            {
                throw new NfeSoapException("Could not find MensagemXML element in SOAP response");
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
                throw new NfeSoapException("MensagemXML element is empty in SOAP response");
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

        var pedido = new PedidoEnvioLoteRPS
        {
            Cabecalho = new PedidoEnvioLoteRPSCabecalho
            {
                Versao = long.Parse(_options.Versao.Replace(".", "")),
                CPFCNPJRemetente = MapCpfCnpjToTpCPFCNPJ(prestador.CpfCnpj),
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
            AliquotaServicos = rps.Item.AliquotaServicos.Value,
            ISSRetido = rps.Item.IssRetido == IssRetido.Sim,
            Discriminacao = rps.Item.Discriminacao,
            ValorIPI = 0.00m,
            ExigibilidadeSuspensa = 0,
            PagamentoParceladoAntecipado = 0,
            NBS = "123456789", // TODO: Get from configuration or RPS item
            IBSCBS = CreateDefaultIBSCBS()
        };

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

    private RetornoEnvioLoteRpsResult MapRetornoEnvioLoteRPSToResult(RetornoEnvioLoteRPS retorno)
    {
        var result = new RetornoEnvioLoteRpsResult
        {
            Sucesso = retorno.Cabecalho.Sucesso,
            Alertas = retorno.Alerta.Select(MapTpEventoToEvento).ToList(),
            Erros = retorno.Erro.Select(MapTpEventoToEvento).ToList(),
            ChavesNFeRPS = retorno.ChaveNFeRPS.Select(MapTpChaveNFeRPSToNfeRpsKeyPair).ToList()
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

    private tpIBSCBS CreateDefaultIBSCBS()
    {
        // Create a default IBSCBS structure
        // TODO: This should be configurable or come from RPS item
        return new tpIBSCBS
        {
            finNFSe = 0, // NFS-e regular
            indFinal = 0, // Não é consumidor final
            cIndOp = "123456", // TODO: Get from configuration
            indDest = 0, // Não informado
            valores = new tpValores
            {
                trib = new tpTrib
                {
                    gIBSCBS = new tpGIBSCBS
                    {
                        cClassTrib = "123456" // TODO: Get from configuration
                    }
                }
            }
        };
    }

    #endregion
}
