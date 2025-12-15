namespace Microled.Nfe.Service.Domain.ValueObjects;

/// <summary>
/// Value object representing a CPF (Brazilian individual tax ID)
/// </summary>
public sealed class Cpf
{
    public string Value { get; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Cpf Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CPF cannot be null or empty", nameof(value));

        // Remove formatting
        var cleanValue = value.Replace(".", "").Replace("-", "").Trim();

        if (cleanValue.Length != 11)
            throw new ArgumentException("CPF must have 11 digits", nameof(value));

        if (!cleanValue.All(char.IsDigit))
            throw new ArgumentException("CPF must contain only digits", nameof(value));

        // TODO: Add CPF validation algorithm if needed

        return new Cpf(cleanValue);
    }

    public string GetFormatted() => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is Cpf other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

