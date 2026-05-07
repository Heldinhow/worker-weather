namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with a currency.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "USDC")
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), $"Money amount cannot be negative, got {amount}");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        return new Money(Math.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "USDC") => new(0m, currency);
    public static Money FromShares(decimal shares, decimal price) => Create(shares * price);

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money a, decimal scalar) => new(a.Amount * scalar, a.Currency);
    public static Money operator /(Money a, decimal scalar) => new(a.Amount / scalar, a.Currency);

    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator ==(Money a, Money b) => a.Amount == b.Amount && a.Currency == b.Currency;
    public static bool operator !=(Money a, Money b) => a.Amount != b.Amount || a.Currency != b.Currency;

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot operate on Money with different currencies: {a.Currency} vs {b.Currency}");
    }

    public static implicit operator decimal(Money m) => m.Amount;
    public override string ToString() => $"{Amount:N2} {Currency}";

    public bool Equals(Money other) => Amount == other.Amount && Currency == other.Currency;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);
}
