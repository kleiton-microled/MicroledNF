namespace Microled.Nfe.Service.Domain.Enums;

/// <summary>
/// Lifecycle status for a persisted fiscal note.
/// </summary>
public enum NotaFiscalStatus
{
    Pending,
    Generated,
    Sent,
    Authorized,
    Rejected,
    Cancelled,
    Error,
    Processing
}
