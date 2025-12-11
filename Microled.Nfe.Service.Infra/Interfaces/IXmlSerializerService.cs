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
    /// Deserializes XML string to an object
    /// </summary>
    T Deserialize<T>(string xml) where T : class;
}

