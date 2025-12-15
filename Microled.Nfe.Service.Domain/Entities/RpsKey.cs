using Microled.Nfe.Service.Domain.ValueObjects;

namespace Microled.Nfe.Service.Domain.Entities;

/// <summary>
/// RPS key identifier
/// </summary>
public sealed class RpsKey
{
    public long InscricaoPrestador { get; }
    public string? SerieRps { get; }
    public long NumeroRps { get; }

    public RpsKey(long inscricaoPrestador, long numeroRps, string? serieRps = null)
    {
        if (inscricaoPrestador <= 0)
            throw new ArgumentException("InscricaoPrestador must be greater than zero", nameof(inscricaoPrestador));

        if (numeroRps <= 0)
            throw new ArgumentException("NumeroRps must be greater than zero", nameof(numeroRps));

        InscricaoPrestador = inscricaoPrestador;
        NumeroRps = numeroRps;
        SerieRps = serieRps;
    }

    public override string ToString() => 
        $"{InscricaoPrestador}-{SerieRps ?? "NF"}-{NumeroRps}";

    public override bool Equals(object? obj) => 
        obj is RpsKey other && 
        InscricaoPrestador == other.InscricaoPrestador && 
        NumeroRps == other.NumeroRps && 
        SerieRps == other.SerieRps;

    public override int GetHashCode() => 
        HashCode.Combine(InscricaoPrestador, NumeroRps, SerieRps);
}

