using System.Numerics;

namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents a probability value between 0 and 1 (inclusive).
/// </summary>
public readonly struct Probability : IEquatable<Probability>, IComparable<Probability>
{
    public decimal Value { get; }

    private Probability(decimal value) => Value = value;

    public static Probability Create(decimal value)
    {
        if (value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value), $"Probability must be between 0 and 1, got {value}");
        return new Probability(Math.Round(value, 6));
    }

    public static Probability One => new(1m);
    public static Probability Zero => new(0m);
    public static Probability FiftyPercent => new(0.5m);
    public static Probability NinetyNinePercent => new(0.99m);
    public static Probability NinetyFivePercent => new(0.95m);

    public static Probability operator +(Probability a, Probability b) => new(a.Value + b.Value);
    public static Probability operator -(Probability a, Probability b) => new(a.Value - b.Value);
    public static Probability operator *(Probability a, Probability b) => new(a.Value * b.Value);
    public static Probability operator *(Probability a, decimal scalar) => new(a.Value * scalar);
    public static Probability operator /(Probability a, decimal scalar) => new(a.Value / scalar);

    public static bool operator <(Probability a, Probability b) => a.Value < b.Value;
    public static bool operator >(Probability a, Probability b) => a.Value > b.Value;
    public static bool operator <=(Probability a, Probability b) => a.Value <= b.Value;
    public static bool operator >=(Probability a, Probability b) => a.Value >= b.Value;
    public static bool operator ==(Probability a, Probability b) => a.Value == b.Value;
    public static bool operator !=(Probability a, Probability b) => a.Value != b.Value;

    public static implicit operator decimal(Probability p) => p.Value;
    public static explicit operator double(Probability p) => (double)p.Value;
    public static explicit operator float(Probability p) => (float)p.Value;

    public bool Equals(Probability other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Probability other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Probability other) => Value.CompareTo(other.Value);
    public override string ToString() => $"{Value:P2}";
}
