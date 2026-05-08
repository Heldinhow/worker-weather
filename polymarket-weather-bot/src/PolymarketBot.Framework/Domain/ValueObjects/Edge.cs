using PolymarketBot.Framework.Domain.Entities;

namespace PolymarketBot.Framework.Domain.ValueObjects;

public sealed partial class Edge : IEquatable<Edge>
{
    public decimal Value { get; }
    public Direction Direction { get; }

    private Edge(decimal value, Direction direction)
    {
        Value = value;
        Direction = direction;
    }

    public static Edge Create(decimal value, Direction direction) => new(value, direction);

    public bool Equals(Edge? other) => other != null && Value == other.Value && Direction == other.Direction;
    public override bool Equals(object? obj) => obj is Edge edge && Equals(edge);
    public override int GetHashCode() => HashCode.Combine(Value, Direction);
}