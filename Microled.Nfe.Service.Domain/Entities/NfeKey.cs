namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// NFe key identifier
/// </summary>
public sealed class NfeKey
{
    public long InscricaoPrestador { get; }
    public long NumeroNFe { get; }
    public string? CodigoVerificacao { get; }
    public string? ChaveNotaNacional { get; }

    public NfeKey(long inscricaoPrestador, long numeroNFe, string? codigoVerificacao = null, string? chaveNotaNacional = null)
    {
        if (inscricaoPrestador <= 0)
            throw new ArgumentException("InscricaoPrestador must be greater than zero", nameof(inscricaoPrestador));

        if (numeroNFe <= 0)
            throw new ArgumentException("NumeroNFe must be greater than zero", nameof(numeroNFe));

        InscricaoPrestador = inscricaoPrestador;
        NumeroNFe = numeroNFe;
        CodigoVerificacao = codigoVerificacao;
        ChaveNotaNacional = chaveNotaNacional;
    }

    public override string ToString() => 
        $"{InscricaoPrestador}-{NumeroNFe}";

    public override bool Equals(object? obj) => 
        obj is NfeKey other && 
        InscricaoPrestador == other.InscricaoPrestador && 
        NumeroNFe == other.NumeroNFe;

    public override int GetHashCode() => 
        HashCode.Combine(InscricaoPrestador, NumeroNFe);
}

