using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Infrastructure.External.PolymarketClient;

/// <summary>
/// Stub Polymarket client for MVP. Returns synthetic data.
/// </summary>
public class StubPolymarketClient : IPolymarketClient
{
    private readonly Random _random = new(42);

    public async Task<IReadOnlyList<MarketDto>> GetActiveMarketsAsync(CancellationToken ct = default)
    {
        await Task.Delay(1, ct);
        return new List<MarketDto>
        {
            CreateMarket("rain-nyc-may10", "Will it rain in New York on May 10, 2026?", 0.91m, true),
            CreateMarket("rain-london-may10", "Will it rain in London on May 10, 2026?", 0.89m, true),
            CreateMarket("rain-tokyo-may10", "Will it rain in Tokyo on May 10, 2026?", 0.94m, true),
            CreateMarket("rain-paris-may10", "Will it rain in Paris on May 10, 2026?", 0.87m, true),
            CreateMarket("rain-sydney-may10", "Will it rain in Sydney on May 10, 2026?", 0.95m, false),
            CreateMarket("rain-berlin-may10", "Will it rain in Berlin on May 10, 2026?", 0.92m, true),
        };
    }

    public async Task<MarketDto?> GetMarketAsync(string marketId, CancellationToken ct = default)
    {
        await Task.Delay(1, ct);
        var markets = await GetActiveMarketsAsync(ct);
        return markets.FirstOrDefault(m => m.MarketId == marketId || m.Slug == marketId);
    }

    public async Task<OrderBookDto?> GetOrderBookAsync(string tokenId, CancellationToken ct = default)
    {
        await Task.Delay(1, ct);
        return new OrderBookDto(
            TokenId: tokenId,
            BestBid: 0.989m,
            BestAsk: 0.991m,
            Spread: 0.002m,
            Liquidity: 500m,
            Bids: new List<OrderBookLevel>
            {
                new(0.989m, 100m),
                new(0.988m, 50m),
                new(0.985m, 25m)
            },
            Asks: new List<OrderBookLevel>
            {
                new(0.991m, 100m),
                new(0.992m, 50m),
                new(0.995m, 25m)
            });
    }

    public Task<bool> IsConnectedAsync(CancellationToken ct = default)
        => Task.FromResult(true);

    private MarketDto CreateMarket(string slug, string question, decimal yesPrice, bool highLiquidity)
    {
        var noPrice = 1m - yesPrice;
        var spread = 0.001m + (decimal)(_random.NextDouble() * 0.002);
        var bestBid = yesPrice - (spread / 2);
        var bestAsk = yesPrice + (spread / 2);
        var liquidity = highLiquidity ? 1000m + (decimal)(_random.NextDouble() * 500) : 10m + (decimal)(_random.NextDouble() * 20);
        var now = DateTime.UtcNow;

        return new MarketDto(
            MarketId: slug,
            Slug: slug,
            Question: question,
            Status: MarketStatus.Open,
            YesPrice: yesPrice,
            NoPrice: noPrice,
            BestBid: Math.Max(0, Math.Round(bestBid, 4)),
            BestAsk: Math.Min(1, Math.Round(bestAsk, 4)),
            Liquidity: Math.Round(liquidity, 2),
            Volume: highLiquidity ? 5000m + (decimal)(_random.NextDouble() * 10000) : 100m,
            WindowStart: now,
            WindowEnd: now.AddMinutes(5),
            UpTokenId: $"{slug}-up",
            DownTokenId: $"{slug}-down");
    }
}