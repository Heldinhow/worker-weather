namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents the edge (advantage) between model probability and market probability.
/// Positive = edge for buyer, Negative = edge against.
/// </summary>
public readonly struct Edge : IEquatable<Edge>, IComparable<Edge>
{
    public decimal Value { get; }  // In decimal, e.g., 0.02 = 2% edge

    private Edge(decimal value) => Value = value;

    public static Edge Create(decimal value) => new(Math.Round(value, 6));

    public static Edge Calculate(decimal modelProbability, decimal marketProbability)
        => new(modelProbability - marketProbability);

    public static Edge Zero => new(0m);

    public decimal ToBps => Value * 10000m;
    public bool HasEdge => Math.Abs(Value) > 0;
    public bool IsPositive => Value > 0;
    public bool IsNegative => Value < 0;

    public static Edge operator +(Edge a, Edge b) => new(a.Value + b.Value);
    public static Edge operator -(Edge a, Edge b) => new(a.Value - b.Value);
    public static Edge operator *(Edge a, decimal scalar) => new(a.Value * scalar);

    public static bool operator <(Edge a, Edge b) => a.Value < b.Value;
    public static bool operator >(Edge a, Edge b) => a.Value > b.Value;
    public static bool operator ==(Edge a, Edge b) => a.Value == b.Value;
    public static bool operator !=(Edge a, Edge b) => a.Value != b.Value;

    public static implicit operator decimal(Edge e) => e.Value;
    public override string ToString() => $"{Value:+0.00%;-0.00%} ({ToBps:+0;-0} bps)";

    public bool Equals(Edge other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Edge other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(Edge other) => Value.CompareTo(other.Value);
}
