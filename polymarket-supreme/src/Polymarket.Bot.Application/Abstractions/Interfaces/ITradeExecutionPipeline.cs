using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Orchestrates the full trade execution pipeline.
/// </summary>
public interface ITradeExecutionPipeline
{
    /// <summary>
    /// Execute a trade: Signal → Decision → Order → Fill → Trade → Position.
    /// </summary>
    Task<TradeExecutionResult> ExecuteAsync(
        TradeExecutionRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Request for trade execution pipeline.
/// </summary>
public record TradeExecutionRequest(
    Guid ExecutionCycleId,
    Guid DecisionTraceId,
    string MarketId,
    string MarketSlug,
    string OutcomeId,
    OrderSide Side,
    decimal EntryPrice,
    decimal BestBid,
    decimal BestAsk,
    decimal Liquidity,
    decimal Size,
    double? TimeToResolutionSeconds = null,
    decimal? BtcPrice = null,
    decimal? EthPrice = null,
    TrendDecisionContext? TrendContext = null);
