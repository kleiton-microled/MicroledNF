namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// Batch of RPS to be sent
/// </summary>
public sealed class RpsBatch
{
    public IReadOnlyList<Rps> RpsList { get; }
    public DateOnly DataInicio { get; }
    public DateOnly DataFim { get; }
    public bool Transacao { get; }

    public RpsBatch(
        IReadOnlyList<Rps> rpsList,
        DateOnly dataInicio,
        DateOnly dataFim,
        bool transacao = true)
    {
        if (rpsList == null || rpsList.Count == 0)
            throw new ArgumentException("RPS list cannot be null or empty", nameof(rpsList));

        if (rpsList.Count > 50)
            throw new ArgumentException("RPS list cannot contain more than 50 items", nameof(rpsList));

        RpsList = rpsList;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Transacao = transacao;
    }
}

