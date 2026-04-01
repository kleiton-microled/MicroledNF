namespace Microled.Nfe.Service.Infra.Interfaces;

/// <summary>
/// Service for building SOAP envelopes for NFS-e Web Service requests
/// </summary>
public interface ISoapEnvelopeBuilder
{
    /// <summary>
    /// Builds a SOAP envelope with the specified operation name and XML payload
    /// </summary>
    /// <param name="operationName">SOAP operation name (e.g., "EnvioLoteRPS", "ConsultaNFe")</param>
    /// <param name="xmlPayload">XML content to wrap in SOAP envelope</param>
    /// <returns>Complete SOAP envelope as string</returns>
    string Build(string operationName, string xmlPayload);

    /// <summary>
    /// Builds a SOAP envelope for EnvioLoteRPS with VersaoSchema field
    /// </summary>
    /// <param name="xmlPayload">XML content to wrap in SOAP envelope</param>
    /// <param name="versaoSchema">Schema version as integer (e.g., 2)</param>
    /// <returns>Complete SOAP envelope as string</returns>
    string BuildEnvioLoteRPS(string xmlPayload, int versaoSchema);

    /// <summary>
    /// Builds a SOAP envelope for ConsultaNFe using the request wrapper expected by the ASMX endpoint.
    /// </summary>
    string BuildConsultaNFe(string xmlPayload, int versaoSchema);
}

