namespace Microled.Nfe.Service.Domain.ValueObjects;

/// <summary>
/// Value object representing a CNPJ (Brazilian company registration number)
/// </summary>
public sealed class Cnpj
{
    public string Value { get; }

    private Cnpj(string value)
    {
        Value = value;
    }

    public static Cnpj Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CNPJ cannot be null or empty", nameof(value));

        // Remove formatting
        var cleanValue = value.Replace(".", "").Replace("/", "").Replace("-", "").Trim();

        if (cleanValue.Length != 14)
            throw new ArgumentException("CNPJ must have 14 digits", nameof(value));

        if (!cleanValue.All(char.IsDigit))
            throw new ArgumentException("CNPJ must contain only digits", nameof(value));

        // TODO: Add CNPJ validation algorithm if needed

        return new Cnpj(cleanValue);
    }

    public string GetFormatted() => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is Cnpj other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

