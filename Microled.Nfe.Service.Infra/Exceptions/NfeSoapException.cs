namespace Microled.Nfe.Service.Infra.Exceptions;

/// <summary>
/// Exception thrown when a SOAP fault occurs or communication with the NFS-e Web Service fails
/// </summary>
public class NfeSoapException : Exception
{
    public string? FaultCode { get; }
    public string? FaultString { get; }
    public string? FaultDetail { get; }
    public int? HttpStatusCode { get; }

    public NfeSoapException(string message) : base(message)
    {
    }

    public NfeSoapException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public NfeSoapException(string message, string? faultCode, string? faultString, string? faultDetail = null)
        : base(message)
    {
        FaultCode = faultCode;
        FaultString = faultString;
        FaultDetail = faultDetail;
    }

    public NfeSoapException(string message, int httpStatusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
    }
}

