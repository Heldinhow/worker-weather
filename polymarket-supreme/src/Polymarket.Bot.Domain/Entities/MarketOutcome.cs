using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a specific outcome within a market (e.g., YES/UP or NO/DOWN).
/// </summary>
public class MarketOutcome : Entity
{
    public Guid MarketId { get; private set; }
    public string OutcomeId { get; private set; } = string.Empty;  // Token ID on Polymarket CLOB
    public string Name { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public decimal Price { get; private set; }
    public decimal BestBid { get; private set; }
    public decimal BestAsk { get; private set; }
    public decimal Liquidity { get; private set; }
    public decimal Volume24h { get; private set; }

    // Navigation
    private Market? _market;
    public Market? Market => _market;

    // For EF Core
    private MarketOutcome() { }

    public static MarketOutcome Create(
        Guid marketId,
        string outcomeId,
        string name,
        OrderSide side,
        decimal price,
        decimal bestBid = 0,
        decimal bestAsk = 0,
        decimal liquidity = 0,
        decimal volume24h = 0)
    {
        if (string.IsNullOrWhiteSpace(outcomeId))
            throw new ArgumentException("OutcomeId cannot be empty", nameof(outcomeId));

        return new MarketOutcome
        {
            MarketId = marketId,
            OutcomeId = outcomeId,
            Name = name ?? string.Empty,
            Side = side,
            Price = price,
            BestBid = bestBid,
            BestAsk = bestAsk,
            Liquidity = liquidity,
            Volume24h = volume24h
        };
    }

    public void UpdatePrice(decimal price, decimal bestBid = 0, decimal bestAsk = 0)
    {
        Price = price;
        BestBid = bestBid;
        BestAsk = bestAsk;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLiquidity(decimal liquidity, decimal volume24h = 0)
    {
        Liquidity = liquidity;
        Volume24h = volume24h;
        UpdatedAt = DateTime.UtcNow;
    }

    public MarketPrice GetMarketPrice()
        => MarketPrice.Create(Price, BestBid, BestAsk, Liquidity, Volume24h);

    public decimal Spread => BestAsk > 0 && BestBid > 0 ? BestAsk - BestBid : 0;
    public decimal MidPrice => BestBid > 0 && BestAsk > 0 ? (BestBid + BestAsk) / 2 : Price;
    public bool IsNearExpiry => Price > 0.99m || Price < 0.01m;
}
