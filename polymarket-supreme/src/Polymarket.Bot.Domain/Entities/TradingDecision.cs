using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a trading decision after signal evaluation and risk assessment.
/// </summary>
public class TradingDecision : Entity
{
    public Guid SignalId { get; private set; }
    public TradingDecisionStatus Status { get; private set; } = TradingDecisionStatus.Pending;
    public string? RejectionReason { get; private set; }
    public DateTime? DecidedAt { get; private set; }

    // Navigation
    private Signal? _signal;
    public Signal? Signal => _signal;

    private Signal? _linkedSignal;
    public Signal? LinkedSignal => _linkedSignal;

    private TradingDecision() { }

    public static TradingDecision Create(Guid signalId)
    {
        return new TradingDecision
        {
            SignalId = signalId,
            Status = TradingDecisionStatus.Pending
        };
    }

    public void Approve()
    {
        Status = TradingDecisionStatus.Approved;
        DecidedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        Status = TradingDecisionStatus.Rejected;
        RejectionReason = reason;
        DecidedAt = DateTime.UtcNow;
    }

    public void Skip(string reason)
    {
        Status = TradingDecisionStatus.Skipped;
        RejectionReason = reason;
        DecidedAt = DateTime.UtcNow;
    }

    public bool IsApproved => Status == TradingDecisionStatus.Approved;
    public bool IsRejected => Status == TradingDecisionStatus.Rejected;
    public bool IsSkipped => Status == TradingDecisionStatus.Skipped;
    public bool IsPending => Status == TradingDecisionStatus.Pending;
    public bool IsFinal => Status != TradingDecisionStatus.Pending;
}
