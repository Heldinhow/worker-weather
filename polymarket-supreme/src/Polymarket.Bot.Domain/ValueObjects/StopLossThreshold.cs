namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents a stop loss threshold as a percentage (0 < value <= 1).
/// Default: 20% stop loss.
/// </summary>
public readonly struct StopLossThreshold : IEquatable<StopLossThreshold>, IComparable<StopLossThreshold>
{
    public decimal Value { get; }  // e.g., 0.20 = 20%

    private StopLossThreshold(decimal value) => Value = value;

    public static StopLossThreshold Create(decimal value)
    {
        if (value <= 0 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value), $"StopLossThreshold must be > 0 and <= 1, got {value}");
        return new StopLossThreshold(Math.Round(value, 4));
    }

    public static StopLossThreshold Default => new(0.20m);
    public static StopLossThreshold Tight => new(0.10m);   // 10%
    public static StopLossThreshold Moderate => new(0.20m); // 20%
    public static StopLossThreshold Wide => new(0.30m);    // 30%

    public decimal ToPercent => Value * 100m;

    public decimal CalculateStopPrice(decimal entryPrice)
    {
        if (entryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(entryPrice), "Entry price must be positive");
        return Math.Round(entryPrice * (1m - Value), 6);
    }

    public static bool operator <(StopLossThreshold a, StopLossThreshold b) => a.Value < b.Value;
    public static bool operator >(StopLossThreshold a, StopLossThreshold b) => a.Value > b.Value;
    public static bool operator ==(StopLossThreshold a, StopLossThreshold b) => a.Value == b.Value;
    public static bool operator !=(StopLossThreshold a, StopLossThreshold b) => a.Value != b.Value;

    public static implicit operator decimal(StopLossThreshold t) => t.Value;
    public override string ToString() => $"{ToPercent:N0}%";

    public bool Equals(StopLossThreshold other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is StopLossThreshold other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(StopLossThreshold other) => Value.CompareTo(other.Value);
}
