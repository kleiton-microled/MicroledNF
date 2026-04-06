using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Configuration;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.XmlSchemas;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// XML serialization service implementation
/// </summary>
public class XmlSerializerService : IXmlSerializerService
{
    private readonly ILogger<XmlSerializerService> _logger;
    private readonly NfeServiceOptions _options;
    private readonly ICertificateProvider? _certificateProvider;

    public XmlSerializerService(ILogger<XmlSerializerService> logger)
    {
        _logger = logger;
        _options = new NfeServiceOptions();
        _certificateProvider = null;
    }

    public XmlSerializerService(
        ILogger<XmlSerializerService> logger,
        IOptions<NfeServiceOptions> options,
        ICertificateProvider? certificateProvider = null)
    {
        _logger = logger;
        _options = options?.Value ?? new NfeServiceOptions();
        _certificateProvider = certificateProvider;
    }

    public string Serialize<T>(T obj) where T : class
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            // Add namespaces based on the root element
            var rootAttr = typeof(T).GetCustomAttributes(typeof(XmlRootAttribute), false)
                .FirstOrDefault() as XmlRootAttribute;
            
            if (rootAttr != null && !string.IsNullOrEmpty(rootAttr.Namespace))
            {
                namespaces.Add("", rootAttr.Namespace);
            }
            else
            {
                // Check for XmlType attribute
                var typeAttr = typeof(T).GetCustomAttributes(typeof(XmlTypeAttribute), false)
                    .FirstOrDefault() as XmlTypeAttribute;
                
                if (typeAttr != null && !string.IsNullOrEmpty(typeAttr.Namespace))
                {
                    namespaces.Add("", typeAttr.Namespace);
                }
                else
                {
                    namespaces.Add("", "");
                }
            }

            using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            });

            serializer.Serialize(xmlWriter, obj, namespaces);
            return stringWriter.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing object of type {Type}", typeof(T).Name);
            throw;
        }
    }

    public string SerializePedidoEnvioLoteRPS(PedidoEnvioLoteRPS pedido)
    {
        if (pedido == null)
            throw new ArgumentNullException(nameof(pedido));

        try
        {
            const string nfeNamespace = "http://www.prefeitura.sp.gov.br/nfe";
            const string dsNamespace = "http://www.w3.org/2000/09/xmldsig#";
            
            using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            });

            // Root element with namespace and ds: namespace prefix for signature
            xmlWriter.WriteStartElement("PedidoEnvioLoteRPS", nfeNamespace);
            xmlWriter.WriteAttributeString("xmlns", "ds", null, dsNamespace);
            
            // Write Cabecalho with xmlns="" (empty namespace)
            WriteCabecalho(xmlWriter, pedido.Cabecalho);
            
            // Write each RPS with xmlns="" (empty namespace)
            // Determine if we're using schema v2 based on Versao
            // When VersaoSchema=2, IBSCBS is mandatory
            var versao = _options.GetVersaoSchemaNumber();
            var isSchemaV2 = versao >= 2;
            // Always use schema v2 fields when VersaoSchema=2, regardless of UseSchemaV2Fields setting
            var useSchemaV2Fields = _options.UseSchemaV2Fields || isSchemaV2;
            
            foreach (var rps in pedido.RPS)
            {
                // Validate RPS before writing (fail-fast)
                ValidateRPS(rps, isSchemaV2);
                WriteRPS(xmlWriter, rps, useSchemaV2Fields, isSchemaV2);
            }
            
            xmlWriter.WriteEndElement(); // PedidoEnvioLoteRPS
            xmlWriter.Flush();
            
            // Get XML string and ensure no whitespace before <?xml
            var xmlWithoutSignature = stringWriter.ToString().TrimStart();
            
            // Sign the XML and append signature if certificate provider is available and XML signature is enabled
            if (_certificateProvider != null && _options.EnableXmlSignature)
            {
                try
                {
                    var signedXml = SignXmlDocument(xmlWithoutSignature, nfeNamespace, dsNamespace);
                    return signedXml;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sign XML document, returning unsigned XML");
                    return xmlWithoutSignature;
                }
            }
            else if (!_options.EnableXmlSignature)
            {
                _logger.LogDebug("XML signature (ds:Signature) is disabled via configuration");
            }
            
            return xmlWithoutSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing PedidoEnvioLoteRPS");
            throw;
        }
    }

    public string SerializePedidoConsultaNFe(PedidoConsultaNFe pedido)
    {
        if (pedido == null)
            throw new ArgumentNullException(nameof(pedido));

        try
        {
            const string nfeNamespace = "http://www.prefeitura.sp.gov.br/nfe";
            const string dsNamespace = "http://www.w3.org/2000/09/xmldsig#";
            using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            });

            xmlWriter.WriteStartElement("PedidoConsultaNFe", nfeNamespace);
            xmlWriter.WriteAttributeString("xmlns", "ds", null, dsNamespace);

            WriteConsultaCabecalho(xmlWriter, pedido.Cabecalho);

            foreach (var detalhe in pedido.Detalhe)
            {
                WriteConsultaDetalhe(xmlWriter, detalhe);
            }

            xmlWriter.WriteEndElement(); // PedidoConsultaNFe
            xmlWriter.Flush();

            var xmlWithoutSignature = stringWriter.ToString().TrimStart();

            if (_certificateProvider != null && _options.EnableXmlSignature)
            {
                try
                {
                    var signedXml = SignXmlDocument(xmlWithoutSignature, nfeNamespace, dsNamespace);
                    return signedXml;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sign PedidoConsultaNFe, returning unsigned XML");
                    return xmlWithoutSignature;
                }
            }
            else if (!_options.EnableXmlSignature)
            {
                _logger.LogDebug("XML signature (ds:Signature) is disabled via configuration");
            }

            return xmlWithoutSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing PedidoConsultaNFe");
            throw;
        }
    }

    public string SerializePedidoCancelamentoNFe(PedidoCancelamentoNFe pedido)
    {
        if (pedido == null)
            throw new ArgumentNullException(nameof(pedido));

        try
        {
            const string nfeNamespace = "http://www.prefeitura.sp.gov.br/nfe";
            const string dsNamespace = "http://www.w3.org/2000/09/xmldsig#";
            using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            });

            xmlWriter.WriteStartElement("PedidoCancelamentoNFe", nfeNamespace);
            xmlWriter.WriteAttributeString("xmlns", "ds", null, dsNamespace);

            WriteCancelamentoCabecalho(xmlWriter, pedido.Cabecalho);

            foreach (var detalhe in pedido.Detalhe)
            {
                WriteCancelamentoDetalhe(xmlWriter, detalhe);
            }

            xmlWriter.WriteEndElement(); // PedidoCancelamentoNFe
            xmlWriter.Flush();

            var xmlWithoutSignature = stringWriter.ToString().TrimStart();

            if (_certificateProvider != null && _options.EnableXmlSignature)
            {
                try
                {
                    var signedXml = SignXmlDocument(xmlWithoutSignature, nfeNamespace, dsNamespace);
                    return signedXml;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sign PedidoCancelamentoNFe, returning unsigned XML");
                    return xmlWithoutSignature;
                }
            }
            else if (!_options.EnableXmlSignature)
            {
                _logger.LogDebug("XML signature (ds:Signature) is disabled via configuration");
            }

            return xmlWithoutSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing PedidoCancelamentoNFe");
            throw;
        }
    }

    private static void WriteConsultaCabecalho(XmlWriter writer, PedidoConsultaNFeCabecalho cabecalho)
    {
        writer.WriteRaw($"<Cabecalho Versao=\"{cabecalho.Versao}\" xmlns=\"\">");
        WriteCPFCNPJ(writer, "CPFCNPJRemetente", cabecalho.CPFCNPJRemetente);
        writer.WriteRaw("</Cabecalho>");
    }

    private static void WriteConsultaDetalhe(XmlWriter writer, PedidoConsultaNFeDetalhe detalhe)
    {
        writer.WriteRaw("<Detalhe xmlns=\"\">");

        if (detalhe.ChaveNFe != null)
        {
            writer.WriteStartElement("ChaveNFe");
            writer.WriteElementString("InscricaoPrestador", detalhe.ChaveNFe.InscricaoPrestador.ToString());
            writer.WriteElementString("NumeroNFe", detalhe.ChaveNFe.NumeroNFe.ToString());

            if (!string.IsNullOrWhiteSpace(detalhe.ChaveNFe.CodigoVerificacao))
            {
                writer.WriteElementString("CodigoVerificacao", detalhe.ChaveNFe.CodigoVerificacao);
            }

            if (!string.IsNullOrWhiteSpace(detalhe.ChaveNFe.ChaveNotaNacional))
            {
                writer.WriteElementString("ChaveNotaNacional", detalhe.ChaveNFe.ChaveNotaNacional);
            }

            writer.WriteEndElement();
        }
        else if (detalhe.ChaveRPS != null)
        {
            WriteChaveRPS(writer, detalhe.ChaveRPS);
        }
        else
        {
            throw new InvalidOperationException("Detalhe de consulta deve conter ChaveNFe ou ChaveRPS.");
        }

        writer.WriteRaw("</Detalhe>");
    }

    private static void WriteCancelamentoCabecalho(XmlWriter writer, PedidoCancelamentoNFeCabecalho cabecalho)
    {
        writer.WriteRaw($"<Cabecalho Versao=\"{cabecalho.Versao}\" xmlns=\"\">");
        WriteCPFCNPJ(writer, "CPFCNPJRemetente", cabecalho.CPFCNPJRemetente);
        writer.WriteElementString("transacao", cabecalho.transacao ? "true" : "false");
        writer.WriteRaw("</Cabecalho>");
    }

    private static void WriteCancelamentoDetalhe(XmlWriter writer, PedidoCancelamentoNFeDetalhe detalhe)
    {
        writer.WriteRaw("<Detalhe xmlns=\"\">");

        writer.WriteStartElement("ChaveNFe");
        writer.WriteElementString("InscricaoPrestador", detalhe.ChaveNFe.InscricaoPrestador.ToString());
        writer.WriteElementString("NumeroNFe", detalhe.ChaveNFe.NumeroNFe.ToString());

        if (!string.IsNullOrWhiteSpace(detalhe.ChaveNFe.CodigoVerificacao))
        {
            writer.WriteElementString("CodigoVerificacao", detalhe.ChaveNFe.CodigoVerificacao);
        }

        if (!string.IsNullOrWhiteSpace(detalhe.ChaveNFe.ChaveNotaNacional))
        {
            writer.WriteElementString("ChaveNotaNacional", detalhe.ChaveNFe.ChaveNotaNacional);
        }

        writer.WriteEndElement();
        writer.WriteElementString("AssinaturaCancelamento", Convert.ToBase64String(detalhe.AssinaturaCancelamento));
        writer.WriteRaw("</Detalhe>");
    }

    public T Deserialize<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML string cannot be null or empty", nameof(xml));

        try
        {
            // Normaliza namespaces inconsistentes retornados pelo webservice (ele injeta xmlns="" em Cabecalho/Erro/Alerta)
            // para bater com os namespaces das classes geradas pelo XSD.
            if (typeof(T) == typeof(RetornoEnvioLoteRPS) ||
                typeof(T) == typeof(RetornoConsulta) ||
                typeof(T) == typeof(RetornoCancelamentoNFe))
            {
                xml = NormalizeRetornoNamespaces(xml);
            }

            var serializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader);

            var result = serializer.Deserialize(xmlReader) as T;
            if (result == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {typeof(T).Name}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing XML to type {Type}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Normaliza namespaces do XML de retorno quando o webservice coloca elementos internos com xmlns=""
    /// (namespace vazio), mas os XSDs esperam:
    /// - Containers (Cabecalho/Erro/Alerta/ChaveNFeRPS/NFe) no namespace NFe
    /// - Conteúdo tipado (Codigo/Descricao/ChaveRPS/InformacoesLote etc.) no namespace NFe/tipos
    /// </summary>
    private static string NormalizeRetornoNamespaces(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return xml;

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            // Se não conseguir parsear, não altera.
            return xml;
        }

        var root = doc.Root;
        if (root == null)
            return xml;

        // Só aplicar nos retornos conhecidos
        var rootName = root.Name.LocalName;
        if (rootName != "RetornoEnvioLoteRPS" &&
            rootName != "RetornoConsulta" &&
            rootName != "RetornoCancelamentoNFe")
        {
            return xml;
        }

        XNamespace nfeNs = "http://www.prefeitura.sp.gov.br/nfe";
        XNamespace tiposNs = "http://www.prefeitura.sp.gov.br/nfe/tipos";

        static void RemoveEmptyDefaultNamespaceDeclaration(XElement el)
        {
            // Remove any xmlns="" declaration (default empty namespace) which would conflict if we set the element name
            // to a non-empty namespace (it would serialize as xmlns="nfe" xmlns="").
            var attrs = el.Attributes()
                .Where(a => a.IsNamespaceDeclaration && string.IsNullOrEmpty(a.Value))
                .ToList();

            foreach (var a in attrs)
                a.Remove();
        }

        // Garante root no namespace NFe
        if (string.IsNullOrEmpty(root.Name.NamespaceName))
            root.Name = nfeNs + root.Name.LocalName;

        // Containers imediatos do retorno ficam em NFe (mesmo se vierem com xmlns="")
        foreach (var child in root.Elements().ToList())
        {
            RemoveEmptyDefaultNamespaceDeclaration(child);
            if (string.IsNullOrEmpty(child.Name.NamespaceName))
                child.Name = nfeNs + child.Name.LocalName;

            switch (child.Name.LocalName)
            {
                case "Cabecalho":
                    // Cabecalho em NFe; seus filhos imediatos em NFe
                    foreach (var cabChild in child.Elements().ToList())
                    {
                        RemoveEmptyDefaultNamespaceDeclaration(cabChild);
                        if (string.IsNullOrEmpty(cabChild.Name.NamespaceName))
                            cabChild.Name = nfeNs + cabChild.Name.LocalName;

                        // InformacoesLote: elemento container em NFe, conteúdo em tipos
                        if (cabChild.Name.LocalName == "InformacoesLote")
                        {
                            foreach (var node in cabChild.DescendantsAndSelf().ToList())
                            {
                                // mantém o container InformacoesLote em NFe
                                if (node == cabChild)
                                    continue;

                                RemoveEmptyDefaultNamespaceDeclaration(node);
                                if (string.IsNullOrEmpty(node.Name.NamespaceName))
                                    node.Name = tiposNs + node.Name.LocalName;
                            }
                        }
                    }
                    break;

                case "Erro":
                case "Alerta":
                case "ChaveNFeRPS":
                case "NFe":
                    // Elemento container em NFe; conteúdo tipado em tipos
                    foreach (var node in child.Descendants().ToList())
                    {
                        RemoveEmptyDefaultNamespaceDeclaration(node);
                        if (string.IsNullOrEmpty(node.Name.NamespaceName))
                            node.Name = tiposNs + node.Name.LocalName;
                    }
                    break;
            }
        }

        // Safety net: strip any remaining xmlns="" declarations anywhere in the document to avoid invalid XML like:
        // <Cabecalho xmlns="http://.../nfe" xmlns="">
        foreach (var a in doc.Descendants().SelectMany(e => e.Attributes())
                     .Where(a => a.IsNamespaceDeclaration && string.IsNullOrEmpty(a.Value))
                     .ToList())
        {
            a.Remove();
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    #region Manual XML Writing Helpers for PedidoEnvioLoteRPS
    
    /// <summary>
    /// Valida o RPS antes de montar o XML (fail-fast).
    /// </summary>
    private static void ValidateRPS(tpRPS rps, bool isSchemaV2)
    {
        // Validação 1: Se atvEvento existir, deve conter TODOS os campos obrigatórios
        if (rps.atvEvento != null)
        {
            if (string.IsNullOrWhiteSpace(rps.atvEvento.xNomeEvt))
                throw new InvalidOperationException("atvEvento.xNomeEvt é obrigatório quando atvEvento está presente");
            
            if (rps.atvEvento.dtIniEvt == default(DateTime))
                throw new InvalidOperationException("atvEvento.dtIniEvt é obrigatório quando atvEvento está presente");
            
            if (rps.atvEvento.dtFimEvt == default(DateTime))
                throw new InvalidOperationException("atvEvento.dtFimEvt é obrigatório quando atvEvento está presente");
            
            if (rps.atvEvento.end == null)
                throw new InvalidOperationException("atvEvento.end é obrigatório quando atvEvento está presente");
        }
        
        // Validação 2: gpPrestacao (schema v2)
        // Decisão de negócio: não suportamos prestação fora do Brasil => NUNCA enviar cPaisPrestacao.
        // Logo, sempre exigimos cLocPrestacao (Brasil) antes do IBSCBS.
        if (!rps.cLocPrestacao.HasValue)
            throw new InvalidOperationException("cLocPrestacao é obrigatório (sistema não suporta cPaisPrestacao / serviços fora do Brasil).");
        
        // Validação 3: IBSCBS é obrigatório no schema v2
        if (isSchemaV2 && rps.IBSCBS == null)
            throw new InvalidOperationException("IBSCBS é obrigatório quando VersaoSchema=2 (schema v2)");
    }
    
    private static void WriteCabecalho(XmlWriter writer, PedidoEnvioLoteRPSCabecalho cabecalho)
    {
        var versao = cabecalho.Versao.ToString();
        
        // Start Cabecalho with xmlns="" (empty namespace) using WriteRaw to bypass XmlWriter's namespace restrictions
        writer.WriteRaw($"<Cabecalho Versao=\"{versao}\" xmlns=\"\">");
        
        // Write CPFCNPJRemetente (inherits namespace from root)
        WriteCPFCNPJ(writer, "CPFCNPJRemetente", cabecalho.CPFCNPJRemetente);
        
        // Write transacao if present
        if (cabecalho.transacao.HasValue)
        {
            writer.WriteElementString("transacao", cabecalho.transacao.Value.ToString().ToLower());
        }
        
        // Write dtInicio (date format: yyyy-MM-dd)
        writer.WriteElementString("dtInicio", cabecalho.dtInicio.ToString("yyyy-MM-dd"));
        
        // Write dtFim (date format: yyyy-MM-dd)
        writer.WriteElementString("dtFim", cabecalho.dtFim.ToString("yyyy-MM-dd"));
        
        // Write QtdRPS
        writer.WriteElementString("QtdRPS", cabecalho.QtdRPS.ToString());
        
        writer.WriteRaw("</Cabecalho>");
    }
    
    private static void WriteRPS(XmlWriter writer, tpRPS rps, bool useSchemaV2Fields, bool isSchemaV2)
    {
        // Start RPS with xmlns="" (empty namespace) using WriteRaw to bypass XmlWriter's namespace restrictions
        writer.WriteRaw("<RPS xmlns=\"\">");
        
        // Write Assinatura (Base64)
        writer.WriteElementString("Assinatura", Convert.ToBase64String(rps.Assinatura));
        
        // Write ChaveRPS (no namespace)
        WriteChaveRPS(writer, rps.ChaveRPS);
        
        // Write TipoRPS
        writer.WriteElementString("TipoRPS", rps.TipoRPS);
        
        // Write DataEmissao (date format: yyyy-MM-dd)
        writer.WriteElementString("DataEmissao", rps.DataEmissao.ToString("yyyy-MM-dd"));
        
        // Write StatusRPS
        writer.WriteElementString("StatusRPS", rps.StatusRPS);
        
        // Write TributacaoRPS
        writer.WriteElementString("TributacaoRPS", rps.TributacaoRPS);

        var tributacaoIsT = string.Equals(rps.TributacaoRPS, "T", StringComparison.OrdinalIgnoreCase);
        var isExportacao = false; // não suportado
        
        // Write values
        writer.WriteElementString("ValorDeducoes", FormatDecimal(rps.ValorDeducoes));
        writer.WriteElementString("ValorPIS", FormatDecimal(rps.ValorPIS));
        writer.WriteElementString("ValorCOFINS", FormatDecimal(rps.ValorCOFINS));
        writer.WriteElementString("ValorINSS", FormatDecimal(rps.ValorINSS));
        writer.WriteElementString("ValorIR", FormatDecimal(rps.ValorIR));
        writer.WriteElementString("ValorCSLL", FormatDecimal(rps.ValorCSLL));
        writer.WriteElementString("CodigoServico", rps.CodigoServico.ToString());
        writer.WriteElementString("AliquotaServicos", FormatAliquota(rps.AliquotaServicos));
        // ISSRetido é xs:boolean no XSD (aceita true/false ou 1/0)
        writer.WriteElementString("ISSRetido", rps.ISSRetido ? "true" : "false");
        
        // Write optional Tomador fields
        if (rps.CPFCNPJTomador != null)
        {
            WriteCPFCNPJNIF(writer, "CPFCNPJTomador", rps.CPFCNPJTomador);
        }
        
        if (rps.InscricaoMunicipalTomador.HasValue)
        {
            writer.WriteElementString("InscricaoMunicipalTomador", rps.InscricaoMunicipalTomador.Value.ToString());
        }
        
        if (rps.InscricaoEstadualTomador.HasValue)
        {
            writer.WriteElementString("InscricaoEstadualTomador", rps.InscricaoEstadualTomador.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(rps.RazaoSocialTomador))
        {
            writer.WriteElementString("RazaoSocialTomador", rps.RazaoSocialTomador);
        }
        
        if (rps.EnderecoTomador != null)
        {
            WriteEndereco(writer, "EnderecoTomador", rps.EnderecoTomador);
        }
        
        if (!string.IsNullOrEmpty(rps.EmailTomador))
        {
            writer.WriteElementString("EmailTomador", rps.EmailTomador);
        }
        
        // Write optional Intermediario fields
        if (rps.CPFCNPJIntermediario != null)
        {
            WriteCPFCNPJ(writer, "CPFCNPJIntermediario", rps.CPFCNPJIntermediario);
        }
        
        if (rps.InscricaoMunicipalIntermediario.HasValue)
        {
            writer.WriteElementString("InscricaoMunicipalIntermediario", rps.InscricaoMunicipalIntermediario.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(rps.ISSRetidoIntermediario))
        {
            writer.WriteElementString("ISSRetidoIntermediario", rps.ISSRetidoIntermediario);
        }
        
        if (!string.IsNullOrEmpty(rps.EmailIntermediario))
        {
            writer.WriteElementString("EmailIntermediario", rps.EmailIntermediario);
        }
        
        // Write Discriminacao
        writer.WriteElementString("Discriminacao", rps.Discriminacao);
        
        // Write required fields after Discriminacao - schema requires these fields to be present
        // Always write ValorCargaTributaria (required by schema, even if 0.00)
        writer.WriteElementString("ValorCargaTributaria", FormatDecimal(rps.ValorCargaTributaria ?? 0m));
        
        // Always write PercentualCargaTributaria (may be required by schema)
        writer.WriteElementString("PercentualCargaTributaria", FormatDecimal(rps.PercentualCargaTributaria ?? 0m));
        
        // Always write FonteCargaTributaria (may be required by schema)
        writer.WriteElementString("FonteCargaTributaria", string.IsNullOrEmpty(rps.FonteCargaTributaria) ? "0" : rps.FonteCargaTributaria);
        
        // MunicipioPrestacao: NÃO enviar quando TributacaoRPS="T" (regra erro 1223)
        // Não preencher com 3550308 nem com zero: deve remover o nó.
        if (!tributacaoIsT && rps.MunicipioPrestacao.HasValue)
        {
            writer.WriteElementString("MunicipioPrestacao", rps.MunicipioPrestacao.Value.ToString());
        }
        
        // ValorTotalRecebido: quando null, NÃO emitir o nó (regra erro 1630 para alguns códigos de serviço como 2919)
        if (rps.ValorTotalRecebido.HasValue)
        {
            writer.WriteElementString("ValorTotalRecebido", FormatDecimal(rps.ValorTotalRecebido.Value));
        }
        
        // PMSP now rejects ValorInicialCobrado (erro 640) on the current layout.
        // Keep backward compatibility for older schema versions only.
        if (rps.ValorInicialCobrado.HasValue && !isSchemaV2)
        {
            writer.WriteElementString("ValorInicialCobrado", FormatDecimal(rps.ValorInicialCobrado.Value));
        }

        if (rps.ValorFinalCobrado.HasValue)
        {
            writer.WriteElementString("ValorFinalCobrado", FormatDecimal(rps.ValorFinalCobrado.Value));
        }
        
        // Always write ValorMulta (required by schema)
        writer.WriteElementString("ValorMulta", FormatDecimal(rps.ValorMulta ?? 0m));
        
        // Always write ValorJuros (required by schema)
        writer.WriteElementString("ValorJuros", FormatDecimal(rps.ValorJuros ?? 0m));
        
        // Always write ValorIPI (required by schema)
        writer.WriteElementString("ValorIPI", FormatDecimal(rps.ValorIPI));
        
        // Always write ExigibilidadeSuspensa (required by schema)
        writer.WriteElementString("ExigibilidadeSuspensa", rps.ExigibilidadeSuspensa.ToString());
        
        // Always write PagamentoParceladoAntecipado (required by schema)
        writer.WriteElementString("PagamentoParceladoAntecipado", rps.PagamentoParceladoAntecipado.ToString());
        
        // Optional fields that MUST come before NBS/atvEvento/cLoc/cPais/IBSCBS (schema order)
        if (rps.CodigoCEI.HasValue)
        {
            writer.WriteElementString("CodigoCEI", rps.CodigoCEI.Value.ToString());
        }
        
        if (rps.MatriculaObra.HasValue)
        {
            writer.WriteElementString("MatriculaObra", rps.MatriculaObra.Value.ToString());
        }
        
        if (rps.NumeroEncapsulamento.HasValue)
        {
            writer.WriteElementString("NumeroEncapsulamento", rps.NumeroEncapsulamento.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(rps.NCM))
        {
            writer.WriteElementString("NCM", rps.NCM);
        }

        // Always write NBS (required by schema, comes before cLocPrestacao/IBSCBS)
        writer.WriteElementString("NBS", string.IsNullOrEmpty(rps.NBS) ? "000000000" : rps.NBS);
        
        // gpPrestacao (schema v2) - MUST come before IBSCBS (schema order)
        // Decisão de negócio: não suportamos cPaisPrestacao => nunca escrever.
        // Mantemos apenas cLocPrestacao (Brasil).
        if (!rps.cLocPrestacao.HasValue)
            throw new InvalidOperationException("cLocPrestacao é obrigatório (sistema não suporta cPaisPrestacao / serviços fora do Brasil).");

        writer.WriteElementString("cLocPrestacao", rps.cLocPrestacao.Value.ToString());
        
        // Schema V2 fields - IBSCBS is mandatory and MUST come after cLoc/cPais (schema order)
        if (useSchemaV2Fields || isSchemaV2)
        {
            if (rps.IBSCBS == null)
                throw new InvalidOperationException("IBSCBS não pode ser null quando VersaoSchema=2");
            
            WriteIBSCBS(writer, rps.IBSCBS);
        }
        
        writer.WriteRaw("</RPS>");
    }
    
    private static void WriteCPFCNPJ(XmlWriter writer, string elementName, tpCPFCNPJ cpfCnpj)
    {
        writer.WriteStartElement(elementName);
        
        if (!string.IsNullOrEmpty(cpfCnpj.CPF))
        {
            writer.WriteElementString("CPF", cpfCnpj.CPF);
        }
        
        if (!string.IsNullOrEmpty(cpfCnpj.CNPJ))
        {
            writer.WriteElementString("CNPJ", cpfCnpj.CNPJ);
        }
        
        writer.WriteEndElement();
    }
    
    private static void WriteCPFCNPJNIF(XmlWriter writer, string elementName, tpCPFCNPJNIF cpfCnpjNif)
    {
        writer.WriteStartElement(elementName);
        
        if (!string.IsNullOrEmpty(cpfCnpjNif.CPF))
        {
            writer.WriteElementString("CPF", cpfCnpjNif.CPF);
        }
        
        if (!string.IsNullOrEmpty(cpfCnpjNif.CNPJ))
        {
            writer.WriteElementString("CNPJ", cpfCnpjNif.CNPJ);
        }
        
        if (!string.IsNullOrEmpty(cpfCnpjNif.NIF))
        {
            writer.WriteElementString("NIF", cpfCnpjNif.NIF);
        }
        
        if (cpfCnpjNif.NaoNIF.HasValue)
        {
            writer.WriteElementString("NaoNIF", cpfCnpjNif.NaoNIF.Value.ToString());
        }
        
        writer.WriteEndElement();
    }
    
    private static void WriteChaveRPS(XmlWriter writer, tpChaveRPS chaveRPS)
    {
        writer.WriteStartElement("ChaveRPS");
        
        writer.WriteElementString("InscricaoPrestador", chaveRPS.InscricaoPrestador.ToString());
        
        if (!string.IsNullOrEmpty(chaveRPS.SerieRPS))
        {
            writer.WriteElementString("SerieRPS", chaveRPS.SerieRPS);
        }
        
        writer.WriteElementString("NumeroRPS", chaveRPS.NumeroRPS.ToString());
        
        writer.WriteEndElement();
    }
    
    private static void WriteEndereco(XmlWriter writer, string elementName, tpEndereco endereco)
    {
        writer.WriteStartElement(elementName);
        
        if (!string.IsNullOrEmpty(endereco.TipoLogradouro))
        {
            writer.WriteElementString("TipoLogradouro", endereco.TipoLogradouro);
        }
        
        if (!string.IsNullOrEmpty(endereco.Logradouro))
        {
            writer.WriteElementString("Logradouro", endereco.Logradouro);
        }
        
        if (!string.IsNullOrEmpty(endereco.NumeroEndereco))
        {
            writer.WriteElementString("NumeroEndereco", endereco.NumeroEndereco);
        }
        
        if (!string.IsNullOrEmpty(endereco.ComplementoEndereco))
        {
            writer.WriteElementString("ComplementoEndereco", endereco.ComplementoEndereco);
        }
        
        if (!string.IsNullOrEmpty(endereco.Bairro))
        {
            writer.WriteElementString("Bairro", endereco.Bairro);
        }
        
        if (endereco.Cidade.HasValue)
        {
            writer.WriteElementString("Cidade", endereco.Cidade.Value.ToString());
        }
        
        if (!string.IsNullOrEmpty(endereco.UF))
        {
            writer.WriteElementString("UF", endereco.UF);
        }
        
        if (endereco.CEP.HasValue)
        {
            writer.WriteElementString("CEP", endereco.CEP.Value.ToString());
        }
        
        // EnderecoExterior is optional and complex - skipping for now
        // TODO: Implement if needed
        
        writer.WriteEndElement();
    }
    
    private static void WriteIBSCBS(XmlWriter writer, tpIBSCBS ibscbs)
    {
        writer.WriteStartElement("IBSCBS");
        
        writer.WriteElementString("finNFSe", ibscbs.finNFSe.ToString());
        writer.WriteElementString("indFinal", ibscbs.indFinal.ToString());
        writer.WriteElementString("cIndOp", ibscbs.cIndOp);
        
        if (ibscbs.tpOper.HasValue)
        {
            writer.WriteElementString("tpOper", ibscbs.tpOper.Value.ToString());
        }
        
        if (ibscbs.gRefNFSe != null)
        {
            WriteGRefNFSe(writer, ibscbs.gRefNFSe);
        }
        
        if (ibscbs.tpEnteGov.HasValue)
        {
            writer.WriteElementString("tpEnteGov", ibscbs.tpEnteGov.Value.ToString());
        }
        
        writer.WriteElementString("indDest", ibscbs.indDest.ToString());
        
        if (ibscbs.dest != null)
        {
            WriteInformacoesPessoa(writer, "dest", ibscbs.dest);
        }
        
        // Write valores (obrigatório)
        WriteValores(writer, ibscbs.valores);
        
        // PMSP 621: com cIndOp 100301 não deve existir imovelobra — não serializar mesmo se o objeto vier preenchido (ex.: front).
        if (ibscbs.imovelobra != null && IbsCbsCIndOpNormalizer.ShouldSerializeImovelObra(ibscbs.cIndOp))
        {
            WriteImovelObra(writer, ibscbs.imovelobra);
        }
        
        writer.WriteEndElement();
    }
    
    private static void WriteGRefNFSe(XmlWriter writer, tpGRefNFSe gRefNFSe)
    {
        writer.WriteStartElement("gRefNFSe");
        
        foreach (var refNfse in gRefNFSe.refNFSe)
        {
            writer.WriteElementString("refNFSe", refNfse);
        }
        
        writer.WriteEndElement();
    }
    
    private static void WriteInformacoesPessoa(XmlWriter writer, string elementName, tpInformacoesPessoa pessoa)
    {
        writer.WriteStartElement(elementName);
        
        if (!string.IsNullOrEmpty(pessoa.CPF))
        {
            writer.WriteElementString("CPF", pessoa.CPF);
        }
        
        if (!string.IsNullOrEmpty(pessoa.CNPJ))
        {
            writer.WriteElementString("CNPJ", pessoa.CNPJ);
        }
        
        if (!string.IsNullOrEmpty(pessoa.NIF))
        {
            writer.WriteElementString("NIF", pessoa.NIF);
        }
        
        if (pessoa.NaoNIF.HasValue)
        {
            writer.WriteElementString("NaoNIF", pessoa.NaoNIF.Value.ToString());
        }
        
        writer.WriteElementString("xNome", pessoa.xNome);
        
        // Endereco and email are optional - skipping for now as not in simple example
        // TODO: Implement if needed
        
        writer.WriteEndElement();
    }
    
    private static void WriteValores(XmlWriter writer, tpValores valores)
    {
        writer.WriteStartElement("valores");
        
        if (valores.gReeRepRes != null)
        {
            WriteGrupoReeRepRes(writer, valores.gReeRepRes);
        }
        
        // Write trib (obrigatório)
        WriteTrib(writer, valores.trib);
        
        writer.WriteEndElement();
    }
    
    private static void WriteGrupoReeRepRes(XmlWriter writer, tpGrupoReeRepRes gReeRepRes)
    {
        writer.WriteStartElement("gReeRepRes");
        
        writer.WriteStartElement("documentos");
        
        foreach (var doc in gReeRepRes.documentos)
        {
            WriteDocumento(writer, doc);
        }
        
        writer.WriteEndElement(); // documentos
        writer.WriteEndElement(); // gReeRepRes
    }
    
    private static void WriteDocumento(XmlWriter writer, tpDocumento doc)
    {
        writer.WriteStartElement("documentos");
        
        // TODO: Implement dFeNacional, docFiscalOutro, docOutro choice - for now using example structure
        // The example shows dFeNacional structure, but it's not in the class definition
        // This is a simplified version - may need adjustment based on actual schema
        
        if (doc.fornec != null)
        {
            WriteFornecedor(writer, doc.fornec);
        }
        
        writer.WriteElementString("dtEmiDoc", doc.dtEmiDoc.ToString("yyyy-MM-dd"));
        writer.WriteElementString("dtCompDoc", doc.dtCompDoc.ToString("yyyy-MM-dd"));
        writer.WriteElementString("tpReeRepRes", doc.tpReeRepRes.ToString("D2"));
        
        if (!string.IsNullOrEmpty(doc.xTpReeRepRes))
        {
            writer.WriteElementString("xTpReeRepRes", doc.xTpReeRepRes);
        }
        
        writer.WriteElementString("vlrReeRepRes", FormatDecimal(doc.vlrReeRepRes));
        
        writer.WriteEndElement();
    }
    
    private static void WriteFornecedor(XmlWriter writer, tpFornecedor fornec)
    {
        writer.WriteStartElement("fornec");
        
        if (!string.IsNullOrEmpty(fornec.CPF))
        {
            writer.WriteElementString("CPF", fornec.CPF);
        }
        
        if (!string.IsNullOrEmpty(fornec.CNPJ))
        {
            writer.WriteElementString("CNPJ", fornec.CNPJ);
        }
        
        if (!string.IsNullOrEmpty(fornec.NIF))
        {
            writer.WriteElementString("NIF", fornec.NIF);
        }
        
        if (fornec.NaoNIF.HasValue)
        {
            writer.WriteElementString("NaoNIF", fornec.NaoNIF.Value.ToString());
        }
        
        writer.WriteElementString("xNome", fornec.xNome);
        
        writer.WriteEndElement();
    }
    
    private static void WriteTrib(XmlWriter writer, tpTrib trib)
    {
        writer.WriteStartElement("trib");
        
        WriteGIBSCBS(writer, trib.gIBSCBS);
        
        writer.WriteEndElement();
    }
    
    private static void WriteGIBSCBS(XmlWriter writer, tpGIBSCBS gIBSCBS)
    {
        writer.WriteStartElement("gIBSCBS");
        
        writer.WriteElementString("cClassTrib", gIBSCBS.cClassTrib);
        
        if (gIBSCBS.gTribRegular != null)
        {
            writer.WriteStartElement("gTribRegular");
            writer.WriteElementString("cClassTribReg", gIBSCBS.gTribRegular.cClassTribReg);
            writer.WriteEndElement();
        }
        
        writer.WriteEndElement();
    }
    
    private static void WriteImovelObra(XmlWriter writer, tpImovelObra imovelobra)
    {
        writer.WriteStartElement("imovelobra");
        
        if (!string.IsNullOrEmpty(imovelobra.inscImobFisc))
        {
            writer.WriteElementString("inscImobFisc", imovelobra.inscImobFisc);
        }
        
        if (!string.IsNullOrEmpty(imovelobra.cCIB))
        {
            writer.WriteElementString("cCIB", imovelobra.cCIB);
        }
        
        if (!string.IsNullOrEmpty(imovelobra.cObra))
        {
            writer.WriteElementString("cObra", imovelobra.cObra);
        }
        
        if (imovelobra.end != null)
        {
            WriteEnderecoSimplesIBSCBS(writer, imovelobra.end);
        }
        
        writer.WriteEndElement();
    }
    
    private static void WriteAtividadeEvento(XmlWriter writer, tpAtividadeEvento atvEvento)
    {
        writer.WriteStartElement("atvEvento");
        
        writer.WriteElementString("xNomeEvt", atvEvento.xNomeEvt);
        writer.WriteElementString("dtIniEvt", atvEvento.dtIniEvt.ToString("yyyy-MM-dd"));
        writer.WriteElementString("dtFimEvt", atvEvento.dtFimEvt.ToString("yyyy-MM-dd"));
        
        // Write endereço do evento (obrigatório)
        WriteEnderecoSimplesIBSCBS(writer, atvEvento.end);
        
        writer.WriteEndElement();
    }
    
    private static void WriteEnderecoSimplesIBSCBS(XmlWriter writer, tpEnderecoSimplesIBSCBS end)
    {
        writer.WriteStartElement("end");
        
        // tpEnderecoSimplesIBSCBS: choice (CEP | endExt) + gpEnderecoBaseIBSCBS (xLgr, nro, xCpl?, xBairro) — todos obrigatórios após a choice, exceto xCpl.
        if (end.endExt != null)
        {
            WriteEnderecoExterior(writer, end.endExt);
        }
        else if (end.CEP.HasValue && end.CEP.Value > 0)
        {
            writer.WriteElementString("CEP", end.CEP.Value.ToString());
        }
        else
        {
            throw new InvalidOperationException("end deve conter CEP (Brasil) ou endExt (Exterior) conforme XSD.");
        }

        var xLgr = string.IsNullOrWhiteSpace(end.xLgr) ? "-" : end.xLgr;
        var nro = string.IsNullOrWhiteSpace(end.nro) ? "S/N" : end.nro;
        var xBairro = string.IsNullOrWhiteSpace(end.xBairro) ? "-" : end.xBairro;
        writer.WriteElementString("xLgr", xLgr);
        writer.WriteElementString("nro", nro);
        if (!string.IsNullOrEmpty(end.xCpl))
        {
            writer.WriteElementString("xCpl", end.xCpl);
        }

        writer.WriteElementString("xBairro", xBairro);
        
        writer.WriteEndElement();
    }

    private static void WriteEnderecoExterior(XmlWriter writer, tpEnderecoExterior endExt)
    {
        // Estrutura conforme tpEnderecoExterior (TiposNFe_v02.xsd)
        writer.WriteStartElement("endExt");
        writer.WriteElementString("cPais", endExt.cPais);
        writer.WriteElementString("cEndPost", endExt.cEndPost);
        writer.WriteElementString("xCidade", endExt.xCidade);
        writer.WriteElementString("xEstProvReg", endExt.xEstProvReg);
        writer.WriteEndElement();
    }
    
    private static string FormatDecimal(decimal value)
    {
        // Format decimal without thousands separator, with dot as decimal separator
        return value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatAliquota(decimal value)
    {
        // NFSe SP aceita aliquotas com 3 casas quando necessário (ex: 0.029).
        // Mantém 2 casas quando possível (ex: 0.05) e usa '.' como separador.
        var rounded3 = Math.Round(value, 3, MidpointRounding.AwayFromZero);
        var rounded2 = Math.Round(value, 2, MidpointRounding.AwayFromZero);

        var fmt = rounded3 == rounded2 ? "0.00" : "0.000";
        return rounded3.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
    }
    
    /// <summary>
    /// Signs an XML document using XMLDSIG and returns the signed XML
    /// </summary>
    private string SignXmlDocument(string xmlContent, string nfeNamespace, string dsNamespace)
    {
        if (_certificateProvider == null)
            throw new InvalidOperationException("Certificate provider is not available");

        var certificate = _certificateProvider.GetCertificate();
        if (!certificate.HasPrivateKey)
            throw new InvalidOperationException("Certificate does not have a private key");

        // Load XML into XmlDocument
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlContent);

        // Create SignedXml object
        var signedXml = new SignedXml(xmlDoc)
        {
            SigningKey = certificate.GetRSAPrivateKey() ?? throw new InvalidOperationException("Certificate does not have RSA private key")
        };

        // Set signature algorithm based on configuration (SHA1 or SHA256)
        var algorithm = _options.XmlSignatureAlgorithm?.ToUpperInvariant() ?? "SHA256";
        if (signedXml.SignedInfo == null)
            throw new InvalidOperationException("SignedInfo is null");
            
        if (algorithm == "SHA1")
        {
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        }
        else if (algorithm == "SHA256")
        {
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        }
        else
        {
            _logger.LogWarning("Unknown XML signature algorithm '{Algorithm}', using SHA256", algorithm);
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        }

        _logger.LogDebug("Using XML signature algorithm: {Algorithm}", algorithm);

        // Create reference to sign the entire document
        var reference = new Reference { Uri = "" };
        
        // Add transform to canonicalize XML
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        
        // Set digest method based on algorithm
        if (algorithm == "SHA1")
        {
            reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        }
        else
        {
            reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
        }
        
        if (reference.DigestMethod == null)
            throw new InvalidOperationException("DigestMethod could not be set");
        
        signedXml.AddReference(reference);

        // Add key info
        var keyInfo = new KeyInfo();
        var keyInfoData = new KeyInfoX509Data(certificate);
        keyInfo.AddClause(keyInfoData);
        signedXml.KeyInfo = keyInfo;

        // Compute signature
        signedXml.ComputeSignature();

        // Get signature XML element
        var signatureElement = signedXml.GetXml();

        // Create a new Signature element with ds: prefix
        var rootElement = xmlDoc.DocumentElement ?? throw new InvalidOperationException("XML document has no root element");
        
        // Create ds:Signature element manually to ensure correct prefix
        var dsSignature = xmlDoc.CreateElement("ds", "Signature", dsNamespace);
        
        // Copy all child nodes from the SignedXml signature element
        foreach (XmlNode node in signatureElement.ChildNodes)
        {
            var importedNode = xmlDoc.ImportNode(node, true);
            dsSignature.AppendChild(importedNode);
        }
        
        // Append ds:Signature to root element
        rootElement.AppendChild(dsSignature);

        // Return signed XML as string
        using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
        {
            Indent = false,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        });
        
        xmlDoc.WriteTo(xmlWriter);
        xmlWriter.Flush();
        
        return stringWriter.ToString();
    }
    
    #endregion

    private class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override Encoding Encoding => _encoding;
    }
}

