using FluentResults;

namespace PolymarketBot.Workers.Weather.Domain.ValueObjects;

public sealed partial class Temperature : IEquatable<Temperature>
{
    public decimal Value { get; }
    public TemperatureUnit Unit { get; }

    private Temperature(decimal value, TemperatureUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    public static Result<Temperature> Create(decimal value, TemperatureUnit unit)
        => Result.Ok(new Temperature(value, unit));

    public decimal ToFahrenheit() => Unit == TemperatureUnit.Fahrenheit ? Value : Value * 9 / 5 + 32;
    public decimal ToCelsius() => Unit == TemperatureUnit.Celsius ? Value : (Value - 32) * 5 / 9;

    public bool Equals(Temperature? other) => other != null && Value == other.Value && Unit == other.Unit;
    public override bool Equals(object? obj) => obj is Temperature temp && Equals(temp);
    public override int GetHashCode() => HashCode.Combine(Value, Unit);
}

public enum TemperatureUnit { Celsius, Fahrenheit }
