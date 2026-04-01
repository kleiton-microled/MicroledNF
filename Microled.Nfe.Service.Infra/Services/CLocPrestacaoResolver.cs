using System.Globalization;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Código IBGE do município (tpCidade no XSD: exatamente 7 dígitos). Valores 0 ou inválidos não podem ser enviados.
/// </summary>
public static class CLocPrestacaoResolver
{
    /// <summary>Município de São Paulo (IBGE) — fallback quando o MDB/config não traz código válido.</summary>
    public const int DefaultSaoPaulo = 3550308;

    public static int Resolve(int? cLocFromIbsCbs, int? codigoMunicipioPrestador)
    {
        if (IsValidIbgeMunicipioCode(cLocFromIbsCbs))
            return cLocFromIbsCbs!.Value;
        if (IsValidIbgeMunicipioCode(codigoMunicipioPrestador))
            return codigoMunicipioPrestador!.Value;
        return DefaultSaoPaulo;
    }

    private static bool IsValidIbgeMunicipioCode(int? code)
    {
        if (code is null or <= 0)
            return false;
        var s = code.Value.ToString(CultureInfo.InvariantCulture);
        return s.Length == 7;
    }
}
