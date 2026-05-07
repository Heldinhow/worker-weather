using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Trace of strategy reasoning for a trading decision.
/// </summary>
public class StrategyReasoningTrace : Entity
{
    public Guid DecisionTraceId { get; private set; }
    public Guid StrategyId { get; private set; }
    public decimal? CompositeScore { get; private set; }
    public SignalAction RecommendedAction { get; private set; } = SignalAction.Skip;
    public string? Reasoning { get; private set; }
    public List<string> PositiveFactors { get; private set; } = new();
    public List<string> NegativeFactors { get; private set; } = new();
    public List<string> BlockingFactors { get; private set; } = new();
    public string? MetadataJson { get; private set; }

    private StrategyReasoningTrace() { }

    public static StrategyReasoningTrace Create(
        Guid decisionTraceId,
        Guid strategyId,
        decimal? compositeScore,
        SignalAction recommendedAction,
        string? reasoning = null)
    {
        return new StrategyReasoningTrace
        {
            DecisionTraceId = decisionTraceId,
            StrategyId = strategyId,
            CompositeScore = compositeScore,
            RecommendedAction = recommendedAction,
            Reasoning = reasoning
        };
    }

    public void AddFactors(
        IEnumerable<string>? positive = null,
        IEnumerable<string>? negative = null,
        IEnumerable<string>? blocking = null)
    {
        if (positive is not null) PositiveFactors.AddRange(positive);
        if (negative is not null) NegativeFactors.AddRange(negative);
        if (blocking is not null) BlockingFactors.AddRange(blocking);
    }
}
