namespace Microled.Nfe.Service.Domain.Exceptions;

public sealed class NotaFiscalNotFoundException : Exception
{
    public NotaFiscalNotFoundException(string message)
        : base(message)
    {
    }
}
