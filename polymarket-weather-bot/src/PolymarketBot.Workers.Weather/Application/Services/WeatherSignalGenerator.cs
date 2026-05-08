using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Framework.Domain.ValueObjects;
using PolymarketBot.Workers.Weather.Domain.Entities;

namespace PolymarketBot.Workers.Weather.Application.Services;

public class WeatherSignalGenerator
{
    private readonly ILogger<WeatherSignalGenerator> _logger;
    private readonly decimal _minEdgeThreshold;
    private readonly decimal _kellyFraction;
    private readonly decimal _balance;

    public WeatherSignalGenerator(ILogger<WeatherSignalGenerator> logger, decimal minEdgeThreshold, decimal kellyFraction, decimal balance)
    {
        _logger = logger;
        _minEdgeThreshold = minEdgeThreshold;
        _kellyFraction = kellyFraction;
        _balance = balance;
    }

    public TradeSignal? Generate(WeatherMarket market, WeatherForecast forecast)
    {
        var modelProb = forecast.Probability;
        var marketPrice = market.YesPrice.Value;
        var edgeValue = modelProb - marketPrice;

        if (Math.Abs(edgeValue) < _minEdgeThreshold)
            return null;

        var direction = edgeValue > 0 ? Direction.YES : Direction.NO;
        var odds = 1 / marketPrice;
        var q = 1 - modelProb;
        var kelly = (modelProb * odds - q) / odds;
        var kellySize = Math.Min(kelly * _balance * _kellyFraction, 20m);

        if (kellySize < 1m)
            return null;

        var edge = Edge.Create(edgeValue, direction);
        var confidence = CalculateConfidence(forecast, market, kellySize);

        return new TradeSignal
        {
            MarketId = market.Id,
            WorkerType = "Weather",
            Direction = direction,
            EntryPrice = Price.Create(marketPrice).Value!,
            Edge = edge,
            Size = kellySize,
            Probability = modelProb,
            Confidence = confidence,
            KellySize = kellySize,
            Timestamp = DateTime.UtcNow
        };
    }

    private decimal CalculateConfidence(WeatherForecast forecast, WeatherMarket market, decimal kellySize)
    {
        decimal confidence = 1.0m;

        // 1. Z-score adjustment: z > 2.0 → +20%, z < 0.5 → -20%
        if (forecast.ZScore > 2.0m) confidence *= 1.20m;
        else if (forecast.ZScore < 0.5m) confidence *= 0.80m;

        // 2. Horizon adjustment: D+0-2 = 100%, D+3-5 = 75%, D+6+ = 50%
        var daysToResolution = (market.EndDate - DateTime.UtcNow).Days;
        if (daysToResolution <= 2) confidence *= 1.0m;
        else if (daysToResolution <= 5) confidence *= 0.75m;
        else confidence *= 0.50m;

        // 3. Spread penalty: spread > 5¢ → ×0.8
        if (market.Spread > 0.05m) confidence *= 0.80m;

        return confidence;
    }
}