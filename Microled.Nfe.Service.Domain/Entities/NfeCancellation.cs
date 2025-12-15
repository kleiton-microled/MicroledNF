namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// NFe cancellation request
/// </summary>
public sealed class NfeCancellation
{
    public NfeKey ChaveNFe { get; }
    public string AssinaturaCancelamento { get; }

    public NfeCancellation(NfeKey chaveNFe, string assinaturaCancelamento)
    {
        ChaveNFe = chaveNFe ?? throw new ArgumentNullException(nameof(chaveNFe));
        AssinaturaCancelamento = assinaturaCancelamento ?? throw new ArgumentNullException(nameof(assinaturaCancelamento));
    }
}

