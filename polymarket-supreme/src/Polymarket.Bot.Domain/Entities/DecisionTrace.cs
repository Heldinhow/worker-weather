using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Root entity for tracing all trading decisions.
/// </summary>
public class DecisionTrace : Entity
{
    public Guid ExecutionCycleId { get; private set; }
    public Guid? SignalId { get; private set; }
    public Guid? TradingDecisionId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? TradeId { get; private set; }
    public Guid? PositionId { get; private set; }
    public DecisionTraceStatus Status { get; private set; } = DecisionTraceStatus.Started;
    public string TraceType { get; private set; } = "trade";  // "trade", "skip", "stop_loss"
    public DateTime? CompletedAt { get; private set; }

    private readonly List<DecisionTraceStep> _steps = new();
    public IReadOnlyList<DecisionTraceStep> Steps => _steps.AsReadOnly();

    private DecisionTrace() { }

    public static DecisionTrace CreateTradeTrace(
        Guid executionCycleId,
        Guid? signalId = null,
        Guid? tradingDecisionId = null)
    {
        return new DecisionTrace
        {
            ExecutionCycleId = executionCycleId,
            SignalId = signalId,
            TradingDecisionId = tradingDecisionId,
            TraceType = "trade"
        };
    }

    public static DecisionTrace CreateSkipTrace(Guid executionCycleId, Guid? signalId = null)
    {
        return new DecisionTrace
        {
            ExecutionCycleId = executionCycleId,
            SignalId = signalId,
            TraceType = "skip"
        };
    }

    public static DecisionTrace CreateStopLossTrace(Guid executionCycleId, Guid positionId, Guid tradeId)
    {
        return new DecisionTrace
        {
            ExecutionCycleId = executionCycleId,
            PositionId = positionId,
            TradeId = tradeId,
            TraceType = "stop_loss"
        };
    }

    public void AddStep(string stepType, string description, string? metadataJson = null)
    {
        var step = DecisionTraceStep.Create(Id, stepType, description, metadataJson);
        _steps.Add(step);
    }

    public void LinkOrder(Guid orderId)
    {
        OrderId = orderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkTrade(Guid tradeId)
    {
        TradeId = tradeId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkPosition(Guid positionId)
    {
        PositionId = positionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = DecisionTraceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsCompleted => Status == DecisionTraceStatus.Completed;
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;
}
