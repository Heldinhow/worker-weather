using FluentResults;

namespace PolymarketBot.Framework.Domain.ValueObjects;

public sealed partial class Price : IEquatable<Price>
{
    public decimal Value { get; }

    private Price(decimal value) => Value = value;

    public static Result<Price> Create(decimal value)
    {
        if (value < 0 || value > 1)
            return Result.Fail($"Price must be between 0 and 1, got {value}");
        return Result.Ok(new Price(value));
    }

    public bool Equals(Price? other) => other != null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Price price && Equals(price);
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator decimal(Price price) => price.Value;
}