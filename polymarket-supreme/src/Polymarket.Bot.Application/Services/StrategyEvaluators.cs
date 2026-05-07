using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Application.Services;

/// <summary>
/// Strategy evaluator for high probability 99c entry.
/// </summary>
public class HighProb99CStrategyEvaluator : IStrategyEvaluator
{
    private readonly StrategySettings _settings;

    public HighProb99CStrategyEvaluator(StrategySettings? settings = null)
    {
        _settings = settings ?? new StrategySettings();
    }

    public StrategyType StrategyType => StrategyType.HighProb99C;

    public StrategyEvaluationResult Evaluate(
        Market market,
        MarketOutcome outcome,
        Strategy strategy,
        StrategyInputContext context)
    {
        var factors = new List<string>();
        var positive = new List<string>();
        var negative = new List<string>();
        var blocking = new List<string>();

        var price = outcome.Price;
        var bestBid = outcome.BestBid;
        var bestAsk = outcome.BestAsk;
        var side = outcome.Side;
        var spread = bestAsk > 0 && bestBid > 0 ? bestAsk - bestBid : 0m;
        var spreadBps = price > 0 ? (spread / price) * 10000m : 0m;

        // Factor 1: Price in 99c range
        if (price >= _settings.EntryPriceThreshold)
        {
            positive.Add($"Price {price:P2} at/above threshold {_settings.EntryPriceThreshold:P2}");
            factors.Add("price_in_range");
        }
        else if (price >= _settings.EntryPriceThreshold - 0.05m)
        {
            positive.Add($"Price {price:P2} near threshold");
            factors.Add("price_near_range");
        }
        else
        {
            blocking.Add($"Price {price:P2} below threshold {_settings.EntryPriceThreshold:P2}");
            factors.Add("price_below_range");
        }

        // Factor 2: Spread check
        if (spreadBps <= _settings.MaxSpreadBps)
        {
            positive.Add($"Spread {spreadBps:N0} bps within limit");
            factors.Add("spread_ok");
        }
        else
        {
            blocking.Add($"Spread {spreadBps:N0} bps exceeds max {_settings.MaxSpreadBps:N0} bps");
            factors.Add("spread_too_wide");
        }

        // Factor 3: Liquidity check
        if (outcome.Liquidity >= _settings.MinLiquidity)
        {
            positive.Add($"Liquidity ${outcome.Liquidity:N2} sufficient");
            factors.Add("liquidity_ok");
        }
        else
        {
            blocking.Add($"Liquidity ${outcome.Liquidity:N2} below min ${_settings.MinLiquidity:N2}");
            factors.Add("low_liquidity");
        }

        // Factor 4: Trend alignment
        if (_settings.RequireTrendAlignment && context?.BtcTrend != null)
        {
            var trend = context.BtcTrend;
            var isBullishTrade = side == OrderSide.Buy;

            if (isBullishTrade)
            {
                if (!trend.ShouldAllowBullishTrade)
                {
                    blocking.Add($"BTC trend {trend.Direction} does not allow bullish trades: {trend.Reason}");
                    factors.Add("trend_blocked_bullish");
                }
                else if (trend.Confidence < _settings.MinTrendConfidence)
                {
                    negative.Add($"BTC trend confidence {trend.Confidence:P0} below threshold");
                    factors.Add("low_trend_confidence");
                }
                else
                {
                    positive.Add($"BTC trend {trend.Direction} supports bullish trade (confidence {trend.Confidence:P0})");
                    factors.Add("trend_supports_bullish");
                }
            }
            else
            {
                if (!trend.ShouldAllowBearishTrade)
                {
                    blocking.Add($"BTC trend {trend.Direction} does not allow bearish trades: {trend.Reason}");
                    factors.Add("trend_blocked_bearish");
                }
                else if (trend.Confidence < _settings.MinTrendConfidence)
                {
                    negative.Add($"BTC trend confidence {trend.Confidence:P0} below threshold");
                    factors.Add("low_trend_confidence");
                }
                else
                {
                    positive.Add($"BTC trend {trend.Direction} supports bearish trade (confidence {trend.Confidence:P0})");
                    factors.Add("trend_supports_bearish");
                }
            }
        }

        // Factor 5: Time to resolution
        if (context?.TimeToResolutionSeconds.HasValue == true)
        {
            var timeSeconds = context.TimeToResolutionSeconds.Value;
            if (timeSeconds < 3)
            {
                blocking.Add($"Time to resolution {timeSeconds:N0}s too short");
                factors.Add("time_too_short");
            }
            else if (timeSeconds > 300)
            {
                negative.Add($"Time to resolution {timeSeconds:N0}s very long");
                factors.Add("time_long");
            }
            else
            {
                positive.Add($"Time to resolution {timeSeconds:N0}s within range");
                factors.Add("time_ok");
            }
        }

        // Determine action
        SignalAction action;
        string? skipReason = null;
        string reasoning;

        if (blocking.Count > 0)
        {
            action = SignalAction.Skip;
            skipReason = $"BLOCKED: {string.Join("; ", blocking)}";
            reasoning = $"SKIP (blocked): {string.Join("; ", blocking)}";
        }
        else if (negative.Count > 2)
        {
            action = SignalAction.Skip;
            skipReason = $"TOO MANY RISKS: {string.Join("; ", negative)}";
            reasoning = $"SKIP (risk factors): {string.Join("; ", negative)}";
        }
        else
        {
            action = side == OrderSide.Buy ? SignalAction.Buy : SignalAction.Sell;
            reasoning = $"ACTIONABLE: {string.Join("; ", positive)}";

            if (negative.Count > 0)
                reasoning += $" | Warnings: {string.Join("; ", negative)}";
        }

        // Calculate metrics
        var modelProbability = price; // For 99c, market price = model
        var edge = modelProbability - 0.5m; // Edge from 50% baseline
        var suggestedSize = CalculateSuggestedSize(edge, modelProbability, price, _settings.MaxTradeAmount);

        return new StrategyEvaluationResult(
            Action: action,
            ModelProbability: modelProbability,
            MarketProbability: price,
            Edge: edge,
            Confidence: positive.Count > blocking.Count + negative.Count ? 0.7m : 0.5m,
            SuggestedSize: suggestedSize,
            Reasoning: reasoning,
            SkipReason: skipReason,
            PositiveFactors: positive,
            NegativeFactors: negative,
            BlockingFactors: blocking);
    }

    private static decimal CalculateSuggestedSize(
        decimal edge,
        decimal probability,
        decimal marketPrice,
        decimal maxTradeSize)
    {
        if (probability <= 0 || probability >= 1 || marketPrice <= 0)
            return 0m;

        // Kelly-based sizing for small edge trades
        var odds = (1m - marketPrice) / marketPrice;
        var kelly = (probability * odds - (1m - probability)) / odds;

        // Fractional Kelly (15%)
        var fractionalKelly = Math.Max(0, Math.Min(kelly * 0.15m, 0.05m));

        // Apply to bankroll (assume $10K bankroll for now)
        var bankroll = 10000m;
        var size = fractionalKelly * bankroll;

        return Math.Min(size, maxTradeSize);
    }
}

/// <summary>
/// Strategy settings.
/// </summary>
public class StrategySettings
{
    public decimal EntryPriceThreshold { get; set; } = 0.99m;
    public decimal StopLossPercentage { get; set; } = 0.20m;
    public decimal MinEdgeBps { get; set; } = 10m;
    public decimal MinTrendConfidence { get; set; } = 0.5m;
    public decimal MinTrendStrength { get; set; } = 0.3m;
    public decimal MinLiquidity { get; set; } = 25m;
    public decimal MaxSpreadBps { get; set; } = 100m;
    public decimal MaxTradeAmount { get; set; } = 25m;
    public bool RequireTrendAlignment { get; set; } = true;
}
