namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Trace of a single risk evaluation rule check.
/// </summary>
public class RiskEvaluationTrace : Entity
{
    public Guid DecisionTraceId { get; private set; }
    public string RuleName { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; }
    public string? Reason { get; private set; }
    public decimal? Threshold { get; private set; }
    public decimal? ActualValue { get; private set; }
    public string? MetadataJson { get; private set; }

    private RiskEvaluationTrace() { }

    public static RiskEvaluationTrace Create(
        Guid decisionTraceId,
        string ruleName,
        bool isApproved,
        string? reason = null,
        decimal? threshold = null,
        decimal? actualValue = null)
    {
        return new RiskEvaluationTrace
        {
            DecisionTraceId = decisionTraceId,
            RuleName = ruleName,
            IsApproved = isApproved,
            Reason = reason,
            Threshold = threshold,
            ActualValue = actualValue
        };
    }
}
