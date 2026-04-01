namespace Microled.Nfe.Service.Infra.Interfaces;

/// <summary>
/// Service for XML serialization and deserialization
/// </summary>
public interface IXmlSerializerService
{
    /// <summary>
    /// Serializes an object to XML string
    /// </summary>
    string Serialize<T>(T obj) where T : class;

    /// <summary>
    /// Serializes PedidoEnvioLoteRPS with special namespace handling (empty namespace for child elements)
    /// </summary>
    string SerializePedidoEnvioLoteRPS(XmlSchemas.PedidoEnvioLoteRPS pedido);

    /// <summary>
    /// Serializes PedidoConsultaNFe and appends XML-DSig when enabled.
    /// </summary>
    string SerializePedidoConsultaNFe(XmlSchemas.PedidoConsultaNFe pedido);

    /// <summary>
    /// Serializes PedidoCancelamentoNFe and appends XML-DSig when enabled.
    /// </summary>
    string SerializePedidoCancelamentoNFe(XmlSchemas.PedidoCancelamentoNFe pedido);

    /// <summary>
    /// Deserializes XML string to an object
    /// </summary>
    T Deserialize<T>(string xml) where T : class;
}

