using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Post-hoc review of a trading decision outcome.
/// </summary>
public class DecisionOutcomeReview : Entity
{
    public Guid DecisionTraceId { get; private set; }
    public string? ActualOutcome { get; private set; }  // "win", "loss", "skipped"
    public decimal? ActualSettlementValue { get; private set; }
    public decimal? ExpectedPnl { get; private set; }
    public decimal? ActualPnl { get; private set; }
    public bool? WasCorrect { get; private set; }
    public bool WasSkipped { get; private set; }
    public bool WasMissedOpportunity { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private DecisionOutcomeReview() { }

    public static DecisionOutcomeReview CreateForTrade(
        Guid decisionTraceId,
        string actualOutcome,
        decimal? actualSettlementValue,
        decimal? actualPnl,
        bool wasCorrect)
    {
        return new DecisionOutcomeReview
        {
            DecisionTraceId = decisionTraceId,
            ActualOutcome = actualOutcome,
            ActualSettlementValue = actualSettlementValue,
            ActualPnl = actualPnl,
            WasCorrect = wasCorrect,
            ReviewedAt = DateTime.UtcNow
        };
    }

    public static DecisionOutcomeReview CreateForSkip(
        Guid decisionTraceId,
        bool wasMissedOpportunity = false,
        string? notes = null)
    {
        return new DecisionOutcomeReview
        {
            DecisionTraceId = decisionTraceId,
            WasSkipped = true,
            WasMissedOpportunity = wasMissedOpportunity,
            Notes = notes,
            ReviewedAt = DateTime.UtcNow
        };
    }
}
