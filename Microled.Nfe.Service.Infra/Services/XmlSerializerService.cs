using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Infra.Interfaces;
using Microled.Nfe.Service.Infra.XmlSchemas;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// XML serialization service implementation
/// </summary>
public class XmlSerializerService : IXmlSerializerService
{
    private readonly ILogger<XmlSerializerService> _logger;

    public XmlSerializerService(ILogger<XmlSerializerService> logger)
    {
        _logger = logger;
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
            var serializer = new XmlSerializer(typeof(PedidoEnvioLoteRPS));
            var namespaces = new XmlSerializerNamespaces();
            // Root element has namespace
            namespaces.Add("", "http://www.prefeitura.sp.gov.br/nfe");
            // Add xsd and xsi namespaces like in the example (but we'll remove xsi:nil attributes later)
            namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
            namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            });

            serializer.Serialize(xmlWriter, pedido, namespaces);
            var xml = stringWriter.ToString();

            // Post-process to add xmlns="" to Cabecalho and RPS elements and remove namespaces from their children
            // This is necessary because XmlSerializer doesn't easily support
            // mixed namespace scenarios where parent has namespace but children don't
            
            // Remove ALL xmlns attributes from Cabecalho and RPS opening tags using a comprehensive regex
            // Match: <Cabecalho ... xmlns="..." ... > and remove the xmlns part
            xml = Regex.Replace(xml, @"(<Cabecalho[^>]*?)\s+xmlns=[""'][^""']*[""']([^>]*?>)", "$1$2", RegexOptions.None);
            xml = Regex.Replace(xml, @"(<RPS[^>]*?)\s+xmlns=[""'][^""']*[""']([^>]*?>)", "$1$2", RegexOptions.None);
            
            // Also handle xmlns as first attribute (before other attributes)
            xml = Regex.Replace(xml, @"(<Cabecalho)\s+xmlns=[""'][^""']*[""'](\s+)", "$1$2", RegexOptions.None);
            xml = Regex.Replace(xml, @"(<RPS)\s+xmlns=[""'][^""']*[""'](\s+)", "$1$2", RegexOptions.None);
            
            // Also handle standalone xmlns (only attribute)
            xml = Regex.Replace(xml, @"(<Cabecalho)\s+xmlns=[""'][^""']*[""'](\s*>)", "$1$2", RegexOptions.None);
            xml = Regex.Replace(xml, @"(<RPS)\s+xmlns=[""'][^""']*[""'](\s*>)", "$1$2", RegexOptions.None);
            
            // Now add xmlns="" to Cabecalho and RPS opening tags
            // Insert it right after the element name (before any attributes)
            xml = Regex.Replace(xml, @"(<Cabecalho)(\s+[^>]*?>)", "$1 xmlns=\"\"$2", RegexOptions.None);
            xml = Regex.Replace(xml, @"(<RPS)(\s+[^>]*?>)", "$1 xmlns=\"\"$2", RegexOptions.None);

            // Remove ALL namespace declarations from elements inside Cabecalho and RPS
            // This includes both the tipos namespace and any other namespace declarations
            // Pattern: <ElementName xmlns="..."> -> <ElementName>
            var tiposNamespacePattern = @" xmlns=""http://www\.prefeitura\.sp\.gov\.br/nfe/tipos""";
            xml = Regex.Replace(xml, tiposNamespacePattern, "", RegexOptions.None);
            
            // Also remove any other xmlns declarations that might be present
            // But we need to be careful to only remove them from elements inside Cabecalho and RPS
            // Since we already added xmlns="" to Cabecalho and RPS, we can safely remove
            // xmlns declarations from their children (elements between <Cabecalho xmlns=""> and </Cabecalho>,
            // and between <RPS xmlns=""> and </RPS>)
            
            // Remove xsi:nil attributes (they shouldn't be present if nillable is false in schema)
            xml = Regex.Replace(xml, @"\s+xsi:nil=""[^""]*""", "", RegexOptions.None);

            // Ensure RPS elements at the root level of PedidoEnvioLoteRPS don't have namespace
            // The RPS elements should be direct children of PedidoEnvioLoteRPS and have xmlns=""
            // But wait - we already added xmlns="" to RPS above. Let me check if there's still an issue.
            // The error says RPS is in namespace 'http://www.prefeitura.sp.gov.br/nfe', which means
            // our xmlns="" might not be working. Let me try a different approach - process RPS more carefully.
            
            // Actually, looking at the error more carefully, it says the RPS element itself has the wrong namespace.
            // We need to ensure that ALL RPS elements (not just the first one) have xmlns=""
            // Let's use a more aggressive approach to ensure all RPS opening tags have xmlns=""
            xml = Regex.Replace(xml, @"<RPS(?![^>]*xmlns="""")", "<RPS xmlns=\"\"", RegexOptions.None);

            return xml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing PedidoEnvioLoteRPS");
            throw;
        }
    }

    public T Deserialize<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML string cannot be null or empty", nameof(xml));

        try
        {
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

