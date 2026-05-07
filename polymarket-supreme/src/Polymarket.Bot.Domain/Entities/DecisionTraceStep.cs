namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// A single step in a decision trace.
/// </summary>
public class DecisionTraceStep : Entity
{
    public Guid DecisionTraceId { get; private set; }
    public string StepType { get; private set; } = string.Empty;  // "input_snapshot", "signal_generated", "risk_evaluation", etc.
    public string Description { get; private set; } = string.Empty;
    public string? MetadataJson { get; private set; }

    private DecisionTraceStep() { }

    public static DecisionTraceStep Create(
        Guid decisionTraceId,
        string stepType,
        string description,
        string? metadataJson = null)
    {
        return new DecisionTraceStep
        {
            DecisionTraceId = decisionTraceId,
            StepType = stepType,
            Description = description,
            MetadataJson = metadataJson
        };
    }
}
