using System.Text;
using System.Text.RegularExpressions;

namespace Microled.Nfe.Service.Infra.Services;

public static class IbsCbsCIndOpNormalizer
{
    public const string DefaultCIndOp = "100301";

    private static readonly Regex SixDigitsRegex = new(@"^\d{6}$", RegexOptions.Compiled);

    /// <summary>
    /// PMSP retorno 621: "O grupo de informações relativo ao imóvel não deve constar na NFS-e para o indicador da operação informado."
    /// No exemplo oficial, <c>cIndOp</c> <c>100301</c> não leva <c>imovelobra</c>; já <c>020101</c> pode levar (ver PedidoEnvioLoteRPS_exemplo.txt).
    /// </summary>
    public static bool ShouldSerializeImovelObra(string normalizedCIndOp)
    {
        if (string.IsNullOrEmpty(normalizedCIndOp))
            return false;
        // Operação padrão (serviços em geral / IBS): sem grupo imóvel/obra no XML.
        return !string.Equals(normalizedCIndOp, DefaultCIndOp, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sanitiza removendo qualquer caractere não numérico e valida se ficou com 6 dígitos.
    /// Retorna <see cref="DefaultCIndOp"/> quando vazio/nulo/inválido.
    /// </summary>
    public static string NormalizeOrDefault(string? value)
        => Normalize(value) ?? DefaultCIndOp;

    /// <summary>
    /// Sanitiza removendo qualquer caractere não numérico e valida se ficou com 6 dígitos.
    /// Retorna null quando vazio/nulo/inválido.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (ch >= '0' && ch <= '9')
                sb.Append(ch);
        }

        if (sb.Length == 0)
            return null;

        var digits = sb.ToString();
        return SixDigitsRegex.IsMatch(digits) ? digits : null;
    }
}

