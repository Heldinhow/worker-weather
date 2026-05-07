using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a single execution cycle of the trading bot.
/// </summary>
public class ExecutionCycle : Entity
{
    public Guid BotRunId { get; private set; }
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public int MarketsScanned { get; private set; }
    public int SignalsGenerated { get; private set; }
    public int TradesExecuted { get; private set; }
    public int StopLossesTriggered { get; private set; }
    public int Errors { get; private set; }
    public PnL CyclePnl { get; private set; } = PnL.Zero;
    public string? ErrorMessage { get; private set; }

    private BotRun? _botRun;
    public BotRun? BotRun => _botRun;

    private readonly List<PnLSnapshot> _pnlSnapshots = new();
    public IReadOnlyList<PnLSnapshot> PnlSnapshots => _pnlSnapshots.AsReadOnly();

    private ExecutionCycle() { }

    public static ExecutionCycle Create(Guid botRunId)
    {
        return new ExecutionCycle
        {
            BotRunId = botRunId,
            StartedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordScan(int marketsScanned, int signalsGenerated)
    {
        MarketsScanned = marketsScanned;
        SignalsGenerated = signalsGenerated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordTrade()
    {
        TradesExecuted++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordStopLoss()
    {
        StopLossesTriggered++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordError(string message)
    {
        Errors++;
        ErrorMessage = message;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordPnl(PnL pnl)
    {
        CyclePnl = PnL.Create(CyclePnl.Value + pnl.Value);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSnapshot(PnLSnapshot snapshot)
    {
        _pnlSnapshots.Add(snapshot);
    }

    public bool IsCompleted => CompletedAt.HasValue;
    public TimeSpan Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
}
