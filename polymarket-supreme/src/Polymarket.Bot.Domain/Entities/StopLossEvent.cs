using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a stop loss event that closed a position.
/// </summary>
public class StopLossEvent : Entity
{
    public Guid PositionId { get; private set; }
    public Guid TradeId { get; private set; }
    public Guid DecisionTraceId { get; private set; }
    public decimal EntryPrice { get; private set; }
    public decimal StopLossPrice { get; private set; }
    public decimal ExitPrice { get; private set; }
    public decimal Shares { get; private set; }
    public decimal Size { get; private set; }
    public decimal Slippage { get; private set; }
    public PnL Pnl { get; private set; }
    public DateTime TriggeredAt { get; private set; } = DateTime.UtcNow;
    public string? ExecutionStatus { get; private set; }
    public string? OrderId { get; private set; }
    public string? RejectReason { get; private set; }

    private Position? _position;
    public Position? Position => _position;

    private Trade? _trade;
    public Trade? Trade => _trade;

    private StopLossEvent() { }

    public static StopLossEvent Create(
        Guid positionId,
        Guid tradeId,
        Guid decisionTraceId,
        decimal entryPrice,
        decimal stopLossPrice,
        decimal exitPrice,
        decimal shares,
        decimal size,
        PnL pnl,
        string? executionStatus = null,
        string? orderId = null,
        decimal slippage = 0)
    {
        return new StopLossEvent
        {
            PositionId = positionId,
            TradeId = tradeId,
            DecisionTraceId = decisionTraceId,
            EntryPrice = entryPrice,
            StopLossPrice = stopLossPrice,
            ExitPrice = exitPrice,
            Shares = shares,
            Size = size,
            Slippage = slippage,
            Pnl = pnl,
            ExecutionStatus = executionStatus,
            OrderId = orderId
        };
    }

    public static StopLossEvent CreateRejected(
        Guid positionId,
        Guid tradeId,
        string rejectReason)
    {
        return new StopLossEvent
        {
            PositionId = positionId,
            TradeId = tradeId,
            EntryPrice = 0,
            StopLossPrice = 0,
            ExitPrice = 0,
            Shares = 0,
            Size = 0,
            RejectReason = rejectReason
        };
    }
}
