namespace Microled.Nfe.Service.Infra.Services;

public static class IbsCbsCClassTribValidator
{
    /// <summary>
    /// Layout 2: cClassTrib must be non-empty after trim and contain only digits.
    /// Leading zeros must be preserved (no normalization besides Trim).
    /// </summary>
    public static string ValidateAndGet(string? value = "220")
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

        return trimmed;
    }
}


