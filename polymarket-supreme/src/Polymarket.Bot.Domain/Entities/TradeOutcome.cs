using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Result of a trade after settlement.
/// </summary>
public class TradeOutcome : Entity
{
    public Guid TradeId { get; private set; }
    public Guid DecisionTraceId { get; private set; }
    public string MarketId { get; private set; } = string.Empty;
    public string MarketSlug { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public decimal EntryPrice { get; private set; }
    public decimal? SettlementValue { get; private set; }
    public string Result { get; private set; } = string.Empty;  // "win", "loss", "push", "stop_loss"
    public PnL Pnl { get; private set; }
    public PositionExitReason? ExitReason { get; private set; }
    public DateTime? SettledAt { get; private set; }
    public decimal? EdgeAtEntry { get; private set; }
    public string? TrendDirection { get; private set; }
    public string? MomentumState { get; private set; }
    public string? StrategyType { get; private set; }
    public string? SkipReason { get; private set; }
    public bool WasSkipped { get; private set; }

    private TradeOutcome() { }

    public static TradeOutcome CreateForTrade(
        Guid tradeId,
        Guid decisionTraceId,
        string marketId,
        string marketSlug,
        OrderSide side,
        decimal entryPrice,
        decimal? settlementValue,
        string result,
        PnL pnl,
        PositionExitReason? exitReason = null,
        decimal? edgeAtEntry = null,
        string? trendDirection = null,
        string? momentumState = null,
        string? strategyType = null)
    {
        return new TradeOutcome
        {
            TradeId = tradeId,
            DecisionTraceId = decisionTraceId,
            MarketId = marketId,
            MarketSlug = marketSlug,
            Side = side,
            EntryPrice = entryPrice,
            SettlementValue = settlementValue,
            Result = result,
            Pnl = pnl,
            ExitReason = exitReason,
            SettledAt = DateTime.UtcNow,
            EdgeAtEntry = edgeAtEntry,
            TrendDirection = trendDirection,
            MomentumState = momentumState,
            StrategyType = strategyType
        };
    }

    public static TradeOutcome CreateForSkip(
        Guid decisionTraceId,
        string marketId,
        string marketSlug,
        string skipReason,
        SignalAction suggestedAction,
        decimal? marketPrice = null,
        string? trendDirection = null,
        string? momentumState = null)
    {
        return new TradeOutcome
        {
            DecisionTraceId = decisionTraceId,
            MarketId = marketId,
            MarketSlug = marketSlug,
            Side = OrderSide.Buy,  // placeholder
            EntryPrice = marketPrice ?? 0,
            Result = "skipped",
            Pnl = PnL.Zero,
            SettledAt = DateTime.UtcNow,
            TrendDirection = trendDirection,
            MomentumState = momentumState,
            SkipReason = skipReason,
            WasSkipped = true
        };
    }

    public bool IsWin => Result == "win";
    public bool IsLoss => Result == "loss";
    public bool IsStopLoss => Result == "stop_loss";
    public bool IsSkipped => WasSkipped || Result == "skipped";
}
