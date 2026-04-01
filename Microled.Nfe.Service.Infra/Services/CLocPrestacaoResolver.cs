using System.Globalization;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Código IBGE do município (tpCidade: 7 dígitos). Com <c>cIndOp</c> padrão <see cref="IbsCbsCIndOpNormalizer.DefaultCIndOp"/> (100301) não se usa endereço de imóvel — apenas prestador / valor explícito em IBSCBS.
/// </summary>
public static class CLocPrestacaoResolver
{
    /// <summary>Município de São Paulo (IBGE) — fallback quando nenhum código válido é encontrado.</summary>
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
