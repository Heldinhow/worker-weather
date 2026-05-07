using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Snapshot of PnL state at a point in time.
/// </summary>
public class PnLSnapshot : Entity
{
    public Guid ExecutionCycleId { get; private set; }
    public decimal Bankroll { get; private set; }
    public decimal TotalInvested { get; private set; }
    public PnL TotalPnl { get; private set; }
    public PnL RealizedPnl { get; private set; }
    public PnL UnrealizedPnl { get; private set; }
    public int OpenPositions { get; private set; }
    public int ClosedPositions { get; private set; }
    public int WinningPositions { get; private set; }
    public int LosingPositions { get; private set; }
    public string? MetadataJson { get; private set; }

    private ExecutionCycle? _executionCycle;
    public ExecutionCycle? ExecutionCycle => _executionCycle;

    private PnLSnapshot() { }

    public static PnLSnapshot Create(
        Guid executionCycleId,
        decimal bankroll,
        decimal totalInvested,
        PnL totalPnl,
        PnL realizedPnl,
        PnL unrealizedPnl,
        int openPositions,
        int closedPositions,
        int winningPositions,
        int losingPositions)
    {
        return new PnLSnapshot
        {
            ExecutionCycleId = executionCycleId,
            Bankroll = Math.Round(bankroll, 2),
            TotalInvested = Math.Round(totalInvested, 2),
            TotalPnl = totalPnl,
            RealizedPnl = realizedPnl,
            UnrealizedPnl = unrealizedPnl,
            OpenPositions = openPositions,
            ClosedPositions = closedPositions,
            WinningPositions = winningPositions,
            LosingPositions = losingPositions
        };
    }

    public decimal WinRate => ClosedPositions > 0
        ? (decimal)WinningPositions / ClosedPositions
        : 0;
}
