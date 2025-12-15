namespace Microled.Nfe.Service.Domain.ValueObjects;

/// <summary>
/// Value object representing tax rate (alíquota)
/// </summary>
public sealed class Aliquota
{
    public decimal Value { get; }

    private Aliquota(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Aliquota must be between 0 and 100", nameof(value));

        Value = value;
    }

    public static Aliquota Create(decimal value) => new(value);

    public static Aliquota Zero => new(0);

    public decimal ToDecimal() => Value;

    public override string ToString() => Value.ToString("F4");

    public override bool Equals(object? obj) => obj is Aliquota other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}

