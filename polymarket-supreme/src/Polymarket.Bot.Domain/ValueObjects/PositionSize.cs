namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents the size of a position in shares and notional value.
/// </summary>
public readonly struct PositionSize : IEquatable<PositionSize>
{
    public decimal Shares { get; }
    public decimal Notional { get; }

    private PositionSize(decimal shares, decimal notional)
    {
        Shares = shares;
        Notional = notional;
    }

    public static PositionSize Create(decimal shares, decimal notional)
    {
        if (shares < 0) throw new ArgumentOutOfRangeException(nameof(shares), "Shares cannot be negative");
        if (notional < 0) throw new ArgumentOutOfRangeException(nameof(notional), "Notional cannot be negative");
        return new PositionSize(Math.Round(shares, 4), Math.Round(notional, 2));
    }

    public static PositionSize FromNotional(decimal notional, decimal pricePerShare)
    {
        if (pricePerShare <= 0) throw new ArgumentOutOfRangeException(nameof(pricePerShare), "Price must be positive");
        var shares = notional / pricePerShare;
        return Create(Math.Round(shares, 4), Math.Round(notional, 2));
    }

    public static PositionSize Zero => new(0m, 0m);

    public bool IsEmpty => Shares <= 0 || Notional <= 0;
    public decimal AveragePrice => Shares > 0 ? Notional / Shares : 0;

    public static PositionSize operator +(PositionSize a, PositionSize b)
        => Create(a.Shares + b.Shares, a.Notional + b.Notional);

    public static PositionSize operator -(PositionSize a, PositionSize b)
        => Create(Math.Max(0, a.Shares - b.Shares), Math.Max(0, a.Notional - b.Notional));

    public bool Equals(PositionSize other) => Shares == other.Shares && Notional == other.Notional;
    public override bool Equals(object? obj) => obj is PositionSize other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Shares, Notional);
    public override string ToString() => $"{Shares:N2} shares @ ${Notional:N2}";
}
