using Microled.Nfe.Service.Infra.Interfaces;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Implementation of SOAP envelope builder for NFS-e Web Service
/// </summary>
public class SoapEnvelopeBuilder : ISoapEnvelopeBuilder
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NfeNamespace = "http://www.prefeitura.sp.gov.br/nfe";

    /// <summary>
    /// Builds a SOAP envelope with the specified operation name and XML payload
    /// </summary>
    public string Build(string operationName, string xmlPayload)
    {
        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

        if (string.IsNullOrWhiteSpace(xmlPayload))
            throw new ArgumentException("XML payload cannot be null or empty", nameof(xmlPayload));

        // Escape XML content for CDATA section
        var escapedXml = EscapeXmlForCdata(xmlPayload);

        var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"">
    <soap:Body>
        <{operationName} xmlns=""{NfeNamespace}"">
            <MensagemXML><![CDATA[{escapedXml}]]></MensagemXML>
        </{operationName}>
    </soap:Body>
</soap:Envelope>";

        return soapEnvelope;
    }

    /// <summary>
    /// Escapes XML content for CDATA section
    /// CDATA sections cannot contain "]]>", so we need to escape it
    /// </summary>
    private string EscapeXmlForCdata(string xml)
    {
        return xml.Replace("]]>", "]]]]><![CDATA[>");
    }
}

