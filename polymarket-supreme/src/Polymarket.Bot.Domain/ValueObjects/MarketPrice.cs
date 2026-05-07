namespace Polymarket.Bot.Domain.ValueObjects;

/// <summary>
/// Represents market price data including best bid/ask and spread.
/// </summary>
public readonly struct MarketPrice : IEquatable<MarketPrice>
{
    public decimal Price { get; }
    public decimal BestBid { get; }
    public decimal BestAsk { get; }
    public decimal Spread { get; }
    public decimal Liquidity { get; }
    public decimal Volume { get; }

    private MarketPrice(
        decimal price,
        decimal bestBid,
        decimal bestAsk,
        decimal spread,
        decimal liquidity,
        decimal volume)
    {
        Price = price;
        BestBid = bestBid;
        BestAsk = bestAsk;
        Spread = spread;
        Liquidity = liquidity;
        Volume = volume;
    }

    public static MarketPrice Create(
        decimal price,
        decimal bestBid,
        decimal bestAsk,
        decimal liquidity = 0,
        decimal volume = 0)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), $"Price cannot be negative");
        if (bestBid < 0) throw new ArgumentOutOfRangeException(nameof(bestBid), $"BestBid cannot be negative");
        if (bestAsk < 0) throw new ArgumentOutOfRangeException(nameof(bestAsk), $"BestAsk cannot be negative");
        if (liquidity < 0) throw new ArgumentOutOfRangeException(nameof(liquidity), $"Liquidity cannot be negative");
        if (volume < 0) throw new ArgumentOutOfRangeException(nameof(volume), $"Volume cannot be negative");

        // best_ask must be >= best_bid (spread >= 0)
        if (bestAsk < bestBid)
            throw new ArgumentException($"BestAsk ({bestAsk}) must be >= BestBid ({bestBid})");

        var spread = bestAsk - bestBid;
        return new MarketPrice(
            Math.Round(price, 6),
            Math.Round(bestBid, 6),
            Math.Round(bestAsk, 6),
            Math.Round(spread, 6),
            Math.Round(liquidity, 2),
            Math.Round(volume, 2));
    }

    public static MarketPrice FromMidPrice(decimal midPrice, decimal spreadBps = 10)
    {
        var spread = midPrice * (spreadBps / 10000m);
        var half = spread / 2;
        return Create(
            price: midPrice,
            bestBid: Math.Max(0, midPrice - half),
            bestAsk: midPrice + half,
            liquidity: 0,
            volume: 0);
    }

    public decimal MidPrice => (BestBid + BestAsk) / 2;
    public decimal SpreadBps => Spread > 0 ? (Spread / MidPrice) * 10000 : 0;
    public bool IsNearExpiry => BestBid > 0.99m || BestAsk > 0.99m;

    public bool Equals(MarketPrice other) =>
        Price == other.Price &&
        BestBid == other.BestBid &&
        BestAsk == other.BestAsk &&
        Liquidity == other.Liquidity;
    public override bool Equals(object? obj) => obj is MarketPrice other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Price, BestBid, BestAsk);
    public override string ToString() => $"Price={Price:P2} Bid={BestBid:P2} Ask={BestAsk:P2} Spread={Spread:P4}";
}
