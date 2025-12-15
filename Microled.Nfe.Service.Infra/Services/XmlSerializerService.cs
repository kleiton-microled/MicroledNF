using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Infra.Interfaces;

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

