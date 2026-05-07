using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a trading strategy configuration.
/// </summary>
public class Strategy : Entity
{
    public string Name { get; private set; } = string.Empty;
    public StrategyType Type { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? ParametersJson { get; private set; }

    // Strategy thresholds
    public decimal EntryPriceThreshold { get; private set; } = 0.99m;
    public decimal StopLossPercentage { get; private set; } = 0.20m;
    public decimal MinEdgeBps { get; private set; } = 10m;    // Minimum edge in basis points
    public decimal MinTrendConfidence { get; private set; } = 0.5m;
    public decimal MinTrendStrength { get; private set; } = 0.3m;
    public decimal MinLiquidity { get; private set; } = 25m;
    public decimal MaxSpreadBps { get; private set; } = 100m; // 1% spread max
    public decimal MaxTradeAmount { get; private set; } = 25m;
    public bool RequireTrendAlignment { get; private set; } = true;

    private Strategy() { }

    public static Strategy Create(string name, StrategyType type, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Strategy name cannot be empty", nameof(name));

        return new Strategy
        {
            Name = name,
            Type = type,
            IsEnabled = isEnabled
        };
    }

    public static Strategy CreateHighProb99C()
    {
        var strategy = Create("High Probability 99c", StrategyType.HighProb99C);
        strategy.EntryPriceThreshold = 0.99m;
        strategy.StopLossPercentage = 0.20m;
        strategy.MinEdgeBps = 10m;
        strategy.MinTrendConfidence = 0.5m;
        strategy.MinTrendStrength = 0.3m;
        strategy.MinLiquidity = 25m;
        strategy.MaxSpreadBps = 100m;
        strategy.MaxTradeAmount = 25m;
        strategy.RequireTrendAlignment = true;
        return strategy;
    }

    public void Enable() { IsEnabled = true; }
    public void Disable() { IsEnabled = false; }
    public void UpdateParameters(string json) { ParametersJson = json; UpdatedAt = DateTime.UtcNow; }

    public void UpdateThresholds(
        decimal? entryPriceThreshold = null,
        decimal? stopLossPercentage = null,
        decimal? minEdgeBps = null,
        decimal? minTrendConfidence = null,
        decimal? minTrendStrength = null,
        decimal? minLiquidity = null,
        decimal? maxSpreadBps = null,
        decimal? maxTradeAmount = null,
        bool? requireTrendAlignment = null)
    {
        if (entryPriceThreshold.HasValue) EntryPriceThreshold = entryPriceThreshold.Value;
        if (stopLossPercentage.HasValue) StopLossPercentage = stopLossPercentage.Value;
        if (minEdgeBps.HasValue) MinEdgeBps = minEdgeBps.Value;
        if (minTrendConfidence.HasValue) MinTrendConfidence = minTrendConfidence.Value;
        if (minTrendStrength.HasValue) MinTrendStrength = minTrendStrength.Value;
        if (minLiquidity.HasValue) MinLiquidity = minLiquidity.Value;
        if (maxSpreadBps.HasValue) MaxSpreadBps = maxSpreadBps.Value;
        if (maxTradeAmount.HasValue) MaxTradeAmount = maxTradeAmount.Value;
        if (requireTrendAlignment.HasValue) RequireTrendAlignment = requireTrendAlignment.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}
