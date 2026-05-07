using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Performance tracking for a strategy.
/// </summary>
public class StrategyPerformanceSnapshot : Entity
{
    public Guid StrategyId { get; private set; }
    public DateTime FromDate { get; private set; }
    public DateTime ToDate { get; private set; }
    public int TotalTrades { get; private set; }
    public int WinningTrades { get; private set; }
    public int LosingTrades { get; private set; }
    public decimal WinRate { get; private set; }
    public PnL TotalPnl { get; private set; }
    public PnL AveragePnl { get; private set; }
    public PnL MaxWin { get; private set; }
    public PnL MaxLoss { get; private set; }
    public int SkippedTrades { get; private set; }
    public int MissedOpportunities { get; private set; }
    public string? MetadataJson { get; private set; }

    private StrategyPerformanceSnapshot() { }

    public static StrategyPerformanceSnapshot Create(
        Guid strategyId,
        DateTime fromDate,
        DateTime toDate,
        int totalTrades,
        int winningTrades,
        int losingTrades,
        PnL totalPnl,
        PnL averagePnl,
        PnL maxWin,
        PnL maxLoss,
        int skippedTrades = 0,
        int missedOpportunities = 0)
    {
        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades : 0m;
        return new StrategyPerformanceSnapshot
        {
            StrategyId = strategyId,
            FromDate = fromDate,
            ToDate = toDate,
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            WinRate = winRate,
            TotalPnl = totalPnl,
            AveragePnl = averagePnl,
            MaxWin = maxWin,
            MaxLoss = maxLoss,
            SkippedTrades = skippedTrades,
            MissedOpportunities = missedOpportunities
        };
    }
}
