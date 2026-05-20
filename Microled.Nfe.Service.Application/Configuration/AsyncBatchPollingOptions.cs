namespace Microled.Nfe.Service.Application.Configuration;

public sealed class AsyncBatchPollingOptions
{
    public const string SectionName = "AsyncBatchPolling";

    /// <summary>
    /// When true, after async send returns a protocol, polls ConsultaSituacaoLote and persists authorization.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public int MaxAttempts { get; set; } = 10;

    public int DelaySeconds { get; set; } = 3;
}
