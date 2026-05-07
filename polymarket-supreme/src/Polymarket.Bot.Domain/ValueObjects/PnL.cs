namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents profit and loss as a decimal value.
/// Can be positive (profit) or negative (loss).
/// </summary>
public readonly struct PnL : IEquatable<PnL>, IComparable<PnL>
{
    public decimal Value { get; }
    public bool IsProfit => Value > 0;
    public bool IsLoss => Value < 0;
    public bool IsBreakEven => Value == 0;

    private PnL(decimal value) => Value = Math.Round(value, 2);

    public static PnL Create(decimal value) => new(value);

    public static PnL Calculate(decimal exitNotional, decimal entryNotional)
        => new(exitNotional - entryNotional);

    public static PnL Zero => new(0m);

    public static PnL operator +(PnL a, PnL b) => new(a.Value + b.Value);
    public static PnL operator -(PnL a, PnL b) => new(a.Value - b.Value);
    public static PnL operator *(PnL a, decimal scalar) => new(a.Value * scalar);

    public static bool operator <(PnL a, PnL b) => a.Value < b.Value;
    public static bool operator >(PnL a, PnL b) => a.Value > b.Value;
    public static bool operator <=(PnL a, PnL b) => a.Value <= b.Value;
    public static bool operator >=(PnL a, PnL b) => a.Value >= b.Value;
    public static bool operator ==(PnL a, PnL b) => a.Value == b.Value;
    public static bool operator !=(PnL a, PnL b) => a.Value != b.Value;

    public static implicit operator decimal(PnL p) => p.Value;
    public override string ToString() => $"{(Value >= 0 ? "+" : "")}{Value:C2}";

    public bool Equals(PnL other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is PnL other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(PnL other) => Value.CompareTo(other.Value);
}
