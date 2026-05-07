using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Results;

/// <summary>
/// Result of a fill simulation.
/// </summary>
public record FillSimulationResult(
    bool Success,
    bool Filled,
    decimal FilledPrice,
    decimal FilledShares,
    decimal Slippage,
    decimal Notional,
    string? RejectReason = null,
    bool PartialFill = false);

/// <summary>
/// Result of trade execution pipeline.
/// </summary>
public record TradeExecutionResult(
    bool Success,
    bool Executed,
    Guid? OrderId = null,
    Guid? TradeId = null,
    Guid? PositionId = null,
    decimal? EntryPrice = null,
    decimal? Shares = null,
    decimal? Notional = null,
    decimal? Slippage = null,
    PnLResult? Pnl = null,
    string? RejectReason = null,
    IReadOnlyList<RiskEvaluationResult>? RiskEvaluations = null);

/// <summary>
/// P&L result.
/// </summary>
public record PnLResult(decimal Realized, decimal Unrealized, decimal Total);

/// <summary>
/// Result of a risk evaluation.
/// </summary>
public record RiskEvaluationResult(
    string RuleName,
    bool IsApproved,
    string? Reason = null,
    decimal? Threshold = null,
    decimal? ActualValue = null);

/// <summary>
/// Result of a stop loss check.
/// </summary>
public record StopLossCheckResult(
    bool ShouldTrigger,
    bool PositionFound,
    decimal? CurrentPrice = null,
    decimal? StopLossPrice = null,
    decimal? LossPercentage = null);

/// <summary>
/// Result of a stop loss exit.
/// </summary>
public record StopLossExitResult(
    bool Success,
    decimal? ExitPrice = null,
    decimal? Shares = null,
    decimal? Notional = null,
    PnLResult? Pnl = null,
    string? RejectReason = null);

/// <summary>
/// Result of strategy evaluation.
/// </summary>
public record StrategyEvaluationResult(
    SignalAction Action,
    decimal? ModelProbability = null,
    decimal? MarketProbability = null,
    decimal? Edge = null,
    decimal? Confidence = null,
    decimal? SuggestedSize = null,
    string? Reasoning = null,
    string? SkipReason = null,
    IReadOnlyList<string>? PositiveFactors = null,
    IReadOnlyList<string>? NegativeFactors = null,
    IReadOnlyList<string>? BlockingFactors = null)
{
    public bool IsSkip => Action == SignalAction.Skip;
    public bool IsActionable => Action == SignalAction.Buy || Action == SignalAction.Sell;
    public bool IsApproved => IsActionable;
}
