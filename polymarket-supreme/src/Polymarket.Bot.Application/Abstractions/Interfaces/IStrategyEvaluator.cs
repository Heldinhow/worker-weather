using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Entities;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Evaluates trading strategies and generates signals.
/// </summary>
public interface IStrategyEvaluator
{
    Domain.Enums.StrategyType StrategyType { get; }

    /// <summary>
    /// Evaluate a market and generate a signal.
    /// </summary>
    StrategyEvaluationResult Evaluate(
        Market market,
        MarketOutcome outcome,
        Strategy strategy,
        StrategyInputContext context);
}

/// <summary>
/// Input context for strategy evaluation.
/// </summary>
public record StrategyInputContext(
    decimal BtcPrice,
    decimal EthPrice,
    TrendDecisionContext? BtcTrend,
    TrendDecisionContext? EthTrend,
    double? TimeToResolutionSeconds,
    decimal? Volume24h);
