using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents an order placed in the market.
/// </summary>
public class Order : Entity
{
    public Guid TradingDecisionId { get; private set; }
    public string OrderId { get; private set; } = string.Empty;  // External order ID
    public string MarketId { get; private set; } = string.Empty;
    public string OutcomeId { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal Price { get; private set; }
    public decimal RequestedPrice { get; private set; }
    public decimal Shares { get; private set; }
    public decimal RequestedShares { get; private set; }
    public decimal FilledPrice { get; private set; }
    public decimal Notional { get; private set; }
    public decimal Slippage { get; private set; }
    public string? RejectReason { get; private set; }
    public string? ExecutionStatus { get; private set; }
    public DateTime? FilledAt { get; private set; }

    private TradingDecision? _tradingDecision;
    public TradingDecision? TradingDecision => _tradingDecision;

    private Order() { }

    public static Order Create(
        Guid tradingDecisionId,
        string marketId,
        string outcomeId,
        OrderSide side,
        decimal price,
        decimal shares)
    {
        if (shares <= 0) throw new ArgumentOutOfRangeException(nameof(shares), "Shares must be positive");

        return new Order
        {
            TradingDecisionId = tradingDecisionId,
            MarketId = marketId,
            OutcomeId = outcomeId,
            Side = side,
            Price = price,
            RequestedPrice = price,
            Shares = shares,
            RequestedShares = shares,
            Notional = Math.Round(price * shares, 2)
        };
    }

    public void Fill(decimal filledPrice, decimal shares, decimal slippage = 0)
    {
        if (filledPrice < 0) throw new ArgumentOutOfRangeException(nameof(filledPrice));
        if (shares <= 0) throw new ArgumentOutOfRangeException(nameof(shares));

        FilledPrice = Math.Round(filledPrice, 6);
        Shares = Math.Round(shares, 4);
        Slippage = Math.Round(slippage, 6);
        Notional = Math.Round(filledPrice * shares, 2);
        Status = OrderStatus.Filled;
        FilledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PartialFill(decimal filledPrice, decimal shares, decimal slippage = 0)
    {
        if (filledPrice < 0) throw new ArgumentOutOfRangeException(nameof(filledPrice));
        if (shares <= 0) throw new ArgumentOutOfRangeException(nameof(shares));

        FilledPrice = Math.Round(filledPrice, 6);
        Shares = Math.Round(shares, 4);
        Slippage = Math.Round(slippage, 6);
        Notional = Math.Round(filledPrice * shares, 2);
        Status = OrderStatus.PartiallyFilled;
        FilledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        RejectReason = reason;
        Status = OrderStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsFilled => Status == OrderStatus.Filled;
    public bool IsOpen => Status == OrderStatus.Pending || Status == OrderStatus.PartiallyFilled;
    public bool IsTerminal => Status is OrderStatus.Filled or OrderStatus.Rejected or OrderStatus.Cancelled;
}
