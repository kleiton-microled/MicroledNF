namespace Microled.Nfe.Service.Infra.Services;

public static class IbsCbsCClassTribValidator
{
    /// <summary>
    /// Layout 2: <c>cClassTrib</c> / <c>cClassTribReg</c> (tipos:tpClassificacaoTributaria) — exatamente 6 dígitos no XML.
    /// Valores como "220" são preenchidos à esquerda ("000220").
    /// </summary>
    public static string ValidateAndGet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("IBSCBS cClassTrib is required for layout 2 (Access column IBSCBS_CClassTrib).");

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            throw new InvalidOperationException("IBSCBS cClassTrib is required for layout 2 (Access column IBSCBS_CClassTrib).");

        for (var i = 0; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
            if (ch < '0' || ch > '9')
                throw new InvalidOperationException($"IBSCBS cClassTrib must contain only digits. Got '{value}'.");
        }

        if (trimmed.Length > 6)
        {
            throw new InvalidOperationException(
                $"IBSCBS cClassTrib must have at most 6 digits (schema tpClassificacaoTributaria). Got '{value}'.");
        }

        return trimmed.PadLeft(6, '0');
    }
}


