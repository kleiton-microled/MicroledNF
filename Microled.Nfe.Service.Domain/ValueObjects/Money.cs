namespace Microled.Nfe.Service.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary values
/// </summary>
public sealed class Money
{
    public decimal Value { get; }

    private Money(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Money value cannot be negative", nameof(value));

        Value = value;
    }

    public static Money Create(decimal value) => new(value);

    public static Money Zero => new(0);

    public Money Add(Money other) => new(Value + other.Value);

    public Money Subtract(Money other) => new(Value - other.Value);

    public Money Multiply(decimal factor) => new(Value * factor);

    public string ToString(int decimals = 2) => Value.ToString($"F{decimals}");

    public override string ToString() => ToString(2);

    public override bool Equals(object? obj) => obj is Money other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static implicit operator decimal(Money money) => money.Value;
}

