using System.Text;
using System.Text.RegularExpressions;

namespace Microled.Nfe.Service.Infra.Services;

public static class IbsCbsCIndOpNormalizer
{
    public const string DefaultCIndOp = "100301";

    private static readonly Regex SixDigitsRegex = new(@"^\d{6}$", RegexOptions.Compiled);

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

