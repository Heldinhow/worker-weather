using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a skipped trade that might have been profitable (missed opportunity).
/// </summary>
public class MissedOpportunity : Entity
{
    public Guid DecisionTraceId { get; private set; }
    public string MarketId { get; private set; } = string.Empty;
    public string MarketSlug { get; private set; } = string.Empty;
    public string SkipReason { get; private set; } = string.Empty;
    public SignalAction SuggestedAction { get; private set; }
    public decimal? MarketPriceAtSkip { get; private set; }
    public decimal? EntryPriceAtSkip { get; private set; }
    public string? TrendDirection { get; private set; }
    public string? MomentumState { get; private set; }
    public decimal? Edge { get; private set; }
    public decimal? Size { get; private set; }
    public string? ActualOutcome { get; private set; }
    public decimal? ActualPnl { get; private set; }
    public bool WasMissed { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private MissedOpportunity() { }

    public static MissedOpportunity Create(
        Guid decisionTraceId,
        string marketId,
        string marketSlug,
        string skipReason,
        SignalAction suggestedAction,
        decimal? marketPriceAtSkip,
        decimal? entryPriceAtSkip,
        string? trendDirection = null,
        string? momentumState = null,
        decimal? edge = null,
        decimal? size = null)
    {
        return new MissedOpportunity
        {
            DecisionTraceId = decisionTraceId,
            MarketId = marketId,
            MarketSlug = marketSlug,
            SkipReason = skipReason,
            SuggestedAction = suggestedAction,
            MarketPriceAtSkip = marketPriceAtSkip,
            EntryPriceAtSkip = entryPriceAtSkip,
            TrendDirection = trendDirection,
            MomentumState = momentumState,
            Edge = edge,
            Size = size
        };
    }

    public void MarkAsMissed(string actualOutcome, decimal actualPnl)
    {
        WasMissed = true;
        ActualOutcome = actualOutcome;
        ActualPnl = actualPnl;
        ReviewedAt = DateTime.UtcNow;
    }
}
