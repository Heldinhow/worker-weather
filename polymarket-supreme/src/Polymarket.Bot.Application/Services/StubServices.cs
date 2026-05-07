using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Application.Services;

/// <summary>
/// Stub implementation of Polymarket client for MVP.
/// Returns synthetic but realistic market data.
/// </summary>
public class StubPolymarketClient : IPolymarketClient
{
    private readonly Random _random = new(42);

    public async Task<IReadOnlyList<MarketDto>> GetActiveMarketsAsync(CancellationToken ct = default)
    {
        await Task.Delay(1, ct);

        // Generate synthetic BTC 5-min markets
        var markets = new List<MarketDto>();

        // Active market near 99c
        markets.Add(CreateSyntheticMarket("btc-updown-5m-active", "Will BTC be above 105,000 at 5:00 PM UTC?", 0.99m, true));

        // Active market near 95c
        markets.Add(CreateSyntheticMarket("btc-updown-5m-active2", "Will BTC be above 104,000 at 5:00 PM UTC?", 0.95m, true));

        // Active market near 50c (BTC flat)
        markets.Add(CreateSyntheticMarket("btc-updown-5m-active3", "Will BTC be above 103,000 at 5:00 PM UTC?", 0.51m, true));

        // Active market near 90c
        markets.Add(CreateSyntheticMarket("btc-updown-5m-active4", "Will BTC be above 102,000 at 5:00 PM UTC?", 0.90m, true));

        // Low volume market near 99c
        markets.Add(CreateSyntheticMarket("btc-updown-5m-lowvol", "Will BTC be above 101,000 at 5:00 PM UTC?", 0.99m, false));

        return markets;
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
    {
        return Task.FromResult(true);
    }

    private MarketDto CreateSyntheticMarket(
        string slug,
        string question,
        decimal yesPrice,
        bool highLiquidity)
    {
        var noPrice = 1m - yesPrice;
        var spread = 0.001m + (decimal)_random.NextDouble() * 0.002m;
        var bestBid = yesPrice - (spread / 2);
        var bestAsk = yesPrice + (spread / 2);
        var liquidity = highLiquidity ? 1000m + (decimal)_random.NextDouble() * 500m : 10m + (decimal)_random.NextDouble() * 20m;

        var windowStart = DateTime.UtcNow;
        var windowEnd = windowStart.AddMinutes(5);

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
            Volume: highLiquidity ? 5000m + (decimal)_random.NextDouble() * 10000m : 100m,
            WindowStart: windowStart,
            WindowEnd: windowEnd,
            UpTokenId: $"{slug}-up",
            DownTokenId: $"{slug}-down");
    }
}

/// <summary>
/// Stub implementation of asset price feed for MVP.
/// Returns synthetic but realistic BTC/ETH data.
/// </summary>
public class StubAssetPriceFeed : IAssetPriceFeed
{
    private readonly Random _random = new(42);
    private decimal _btcPrice = 105000m;
    private decimal _ethPrice = 3500m;

    public async Task<AssetPriceDto> GetPriceAsync(AssetSymbol symbol, CancellationToken ct = default)
    {
        await Task.Delay(1, ct);

        // Simulate small price movements
        var jitter = (decimal)(_random.NextDouble() - 0.5) * 50m;
        var price = symbol == AssetSymbol.BTC
            ? _btcPrice + jitter
            : _ethPrice + jitter / 10m;

        if (symbol == AssetSymbol.BTC)
            _btcPrice = price;
        else
            _ethPrice = price;

        return new AssetPriceDto(
            Symbol: symbol,
            Price: Math.Round(price, 2),
            High24h: Math.Round(price * 1.02m, 2),
            Low24h: Math.Round(price * 0.98m, 2),
            Volume24h: symbol == AssetSymbol.BTC ? 50_000_000_000m : 25_000_000_000m,
            Timestamp: DateTime.UtcNow,
            Source: "stub");
    }

    public async Task<IReadOnlyList<AssetPriceDto>> GetPriceHistoryAsync(
        AssetSymbol symbol,
        TrendTimeframe timeframe,
        int limit = 60,
        CancellationToken ct = default)
    {
        await Task.Delay(1, ct);

        var prices = new List<AssetPriceDto>();
        var basePrice = symbol == AssetSymbol.BTC ? _btcPrice : _ethPrice;
        var intervalMinutes = timeframe switch
        {
            TrendTimeframe.OneMinute => 1,
            TrendTimeframe.FiveMinutes => 5,
            TrendTimeframe.FifteenMinutes => 15,
            TrendTimeframe.OneHour => 60,
            _ => 1
        };

        var now = DateTime.UtcNow;
        for (var i = limit - 1; i >= 0; i--)
        {
            var timestamp = now.AddMinutes(-i * intervalMinutes);
            var variation = (decimal)(_random.NextDouble() - 0.5) * basePrice * 0.001m;
            var price = basePrice + variation;

            prices.Add(new AssetPriceDto(
                Symbol: symbol,
                Price: Math.Round(price, 2),
                High24h: null,
                Low24h: null,
                Volume24h: null,
                Timestamp: timestamp,
                Source: "stub"));
        }

        return prices;
    }
}

/// <summary>
/// Stub trend analyzer for MVP.
/// Analyzes price history and generates trend signals.
/// </summary>
public class StubTrendAnalyzer
{
    private readonly IAssetPriceFeed _priceFeed;

    public StubTrendAnalyzer(IAssetPriceFeed priceFeed)
    {
        _priceFeed = priceFeed;
    }

    public async Task<AssetTrendDto> AnalyzeTrendAsync(
        AssetSymbol symbol,
        TrendTimeframe timeframe = TrendTimeframe.OneMinute,
        CancellationToken ct = default)
    {
        var history = await _priceFeed.GetPriceHistoryAsync(symbol, timeframe, 60, ct);
        var priceList = history.Select(p => p.Price).ToList();

        if (priceList.Count < 5)
        {
            return CreateNeutralTrend(symbol, timeframe);
        }

        // Calculate indicators (simplified from Python reference)
        var currentPrice = priceList.Last();
        var rsi = CalculateRSI(priceList);
        var momentum = CalculateMomentum(priceList);
        var vwapDeviation = CalculateVwapDeviation(priceList);
        var smaCrossover = CalculateSmaCrossover(priceList);

        // Determine direction and momentum
        var direction = DetermineDirection(rsi, momentum, vwapDeviation);
        var momentumState = DetermineMomentumState(momentum, rsi);
        var confidence = CalculateConfidence(rsi, momentum, vwapDeviation, smaCrossover);
        var strength = Math.Abs(momentum);

        var shouldAllowBullish = direction == TrendDirection.Up && momentumState is MomentumState.Bullish or MomentumState.StrongBullish;
        var shouldAllowBearish = direction == TrendDirection.Down && momentumState is MomentumState.Bearish or MomentumState.StrongBearish;

        var reason = $"RSI={rsi:N0}, Mom={momentum:+0.00%;-0.00%}, VWAP={vwapDeviation:+0.00%;-0.00%}";

        return new AssetTrendDto(
            Symbol: symbol,
            Timeframe: timeframe,
            Direction: direction,
            Momentum: momentumState,
            TrendStrength: strength,
            Confidence: confidence,
            Rsi: rsi,
            Momentum1m: momentum,
            Momentum5m: CalculateMomentum(priceList.TakeLast(5).ToList()),
            Momentum15m: CalculateMomentum(priceList.TakeLast(15).ToList()),
            VwapDeviation: vwapDeviation,
            SmaCrossover: smaCrossover,
            ShouldAllowBullishTrade: shouldAllowBullish,
            ShouldAllowBearishTrade: shouldAllowBearish,
            Reason: reason);
    }

    private static decimal CalculateRSI(List<decimal> prices, int period = 14)
    {
        if (prices.Count < period + 1) return 50m;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (var i = 1; i < prices.Count; i++)
        {
            var change = prices[i] - prices[i - 1];
            if (change > 0)
                gains.Add(change);
            else
                losses.Add(-change);
        }

        if (gains.Count == 0 || losses.Count == 0) return 50m;

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100m;
        var rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }

    private static decimal CalculateMomentum(List<decimal> prices)
    {
        if (prices.Count < 2) return 0m;
        return (prices.Last() - prices[0]) / prices[0];
    }

    private static decimal CalculateVwapDeviation(List<decimal> prices)
    {
        if (prices.Count == 0) return 0m;
        var avg = prices.Average();
        var current = prices.Last();
        return (current - avg) / avg;
    }

    private static decimal CalculateSmaCrossover(List<decimal> prices)
    {
        if (prices.Count < 15) return 0m;
        var sma5 = prices.TakeLast(5).Average();
        var sma15 = prices.TakeLast(15).Average();
        return (sma5 - sma15) / sma15;
    }

    private static TrendDirection DetermineDirection(decimal rsi, decimal momentum, decimal vwapDev)
    {
        var bullishVotes = 0;
        var bearishVotes = 0;

        if (rsi < 30) bullishVotes += 2;
        else if (rsi > 70) bearishVotes += 2;
        else if (rsi < 50) bullishVotes++;
        else if (rsi > 50) bearishVotes++;

        if (momentum > 0) bullishVotes += 2;
        else if (momentum < 0) bearishVotes += 2;

        if (vwapDev > 0) bullishVotes++;
        else if (vwapDev < 0) bearishVotes++;

        return bullishVotes > bearishVotes ? TrendDirection.Up
            : bearishVotes > bullishVotes ? TrendDirection.Down
            : TrendDirection.Sideways;
    }

    private static MomentumState DetermineMomentumState(decimal momentum, decimal rsi)
    {
        if (momentum > 0.001m && rsi > 60) return MomentumState.StrongBullish;
        if (momentum > 0) return MomentumState.Bullish;
        if (momentum < -0.001m && rsi < 40) return MomentumState.StrongBearish;
        if (momentum < 0) return MomentumState.Bearish;
        return MomentumState.Neutral;
    }

    private static decimal CalculateConfidence(decimal rsi, decimal momentum, decimal vwapDev, decimal smaCrossover)
    {
        var confidence = 0.5m;

        // RSI extreme gives more confidence
        var rsiDist = Math.Abs(rsi - 50) / 50;
        confidence += rsiDist * 0.2m;

        // Momentum magnitude gives more confidence
        confidence += Math.Min(0.3m, Math.Abs(momentum) * 10);

        // All indicators agreeing gives more confidence
        var agreement = 0m;
        if ((rsi < 30 && momentum > 0) || (rsi > 70 && momentum < 0)) agreement += 0.2m;

        return Math.Min(0.95m, Math.Max(0.1m, confidence));
    }

    private static AssetTrendDto CreateNeutralTrend(AssetSymbol symbol, TrendTimeframe timeframe)
    {
        return new AssetTrendDto(
            Symbol: symbol,
            Timeframe: timeframe,
            Direction: TrendDirection.Unknown,
            Momentum: MomentumState.Neutral,
            TrendStrength: 0m,
            Confidence: 0m,
            Rsi: 50m,
            Momentum1m: 0m,
            Momentum5m: 0m,
            Momentum15m: 0m,
            VwapDeviation: 0m,
            SmaCrossover: 0m,
            ShouldAllowBullishTrade: false,
            ShouldAllowBearishTrade: false,
            Reason: "Insufficient data");
    }
}
