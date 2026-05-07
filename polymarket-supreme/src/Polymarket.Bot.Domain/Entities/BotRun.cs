using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a session of bot execution.
/// </summary>
public class BotRun : Entity
{
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }
    public TradingMode Mode { get; private set; } = TradingMode.Paper;
    public decimal InitialBankroll { get; private set; } = 10000m;
    public decimal CurrentBankroll { get; private set; } = 10000m;
    public decimal TotalPnl { get; private set; }
    public int TotalTrades { get; private set; }
    public int WinningTrades { get; private set; }
    public bool IsRunning { get; private set; } = true;

    private readonly List<ExecutionCycle> _cycles = new();
    public IReadOnlyList<ExecutionCycle> Cycles => _cycles.AsReadOnly();

    private BotRun() { }

    public static BotRun Start(TradingMode mode = TradingMode.Paper, decimal initialBankroll = 10000m)
    {
        return new BotRun
        {
            Mode = mode,
            InitialBankroll = initialBankroll,
            CurrentBankroll = initialBankroll
        };
    }

    public ExecutionCycle StartCycle()
    {
        var cycle = ExecutionCycle.Create(Id);
        _cycles.Add(cycle);
        return cycle;
    }

    public void End()
    {
        EndedAt = DateTime.UtcNow;
        IsRunning = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBankroll(decimal pnl)
    {
        CurrentBankroll = Math.Round(CurrentBankroll + pnl, 2);
        TotalPnl = Math.Round(TotalPnl + pnl, 2);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordTrade(bool isWin)
    {
        TotalTrades++;
        if (isWin) WinningTrades++;
    }

    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades : 0;
    public TimeSpan Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : DateTime.UtcNow - StartedAt;
}
