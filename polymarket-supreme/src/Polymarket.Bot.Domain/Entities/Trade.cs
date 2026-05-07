using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a trade execution result.
/// </summary>
public class Trade : Entity
{
    public Guid OrderId { get; private set; }
    public string MarketId { get; private set; } = string.Empty;
    public string MarketTicker { get; private set; } = string.Empty;
    public string EventSlug { get; private set; } = string.Empty;
    public Guid PositionId { get; private set; }
    public OrderSide Side { get; private set; }
    public decimal EntryPrice { get; private set; }
    public decimal Size { get; private set; }  // Notional $
    public decimal Shares { get; private set; }
    public TradeStatus Status { get; private set; } = TradeStatus.Open;
    public bool Settled { get; private set; }
    public DateTime? SettledAt { get; private set; }
    public decimal? SettlementValue { get; private set; }
    public PnL Pnl { get; private set; } = PnL.Zero;
    public string? Result { get; private set; }  // win, loss, push, stop_loss

    // Model data
    public decimal? ModelProbability { get; private set; }
    public decimal? MarketPriceAtEntry { get; private set; }
    public decimal? EdgeAtEntry { get; private set; }
    public string? MarketType { get; private set; }

    private Order? _order;
    public Order? Order => _order;

    private Trade() { }

    public static Trade Create(
        Guid orderId,
        string marketId,
        string marketTicker,
        Guid positionId,
        OrderSide side,
        decimal entryPrice,
        decimal size,
        decimal shares,
        string? marketType = null,
        decimal? modelProbability = null,
        decimal? marketPriceAtEntry = null,
        decimal? edgeAtEntry = null)
    {
        return new Trade
        {
            OrderId = orderId,
            MarketId = marketId,
            MarketTicker = marketTicker,
            PositionId = positionId,
            Side = side,
            EntryPrice = entryPrice,
            Size = size,
            Shares = shares,
            MarketType = marketType,
            ModelProbability = modelProbability,
            MarketPriceAtEntry = marketPriceAtEntry,
            EdgeAtEntry = edgeAtEntry
        };
    }

    public void Settle(decimal settlementValue, string result)
    {
        if (Settled)
            throw new InvalidOperationException("Trade is already settled");

        SettlementValue = settlementValue;
        Result = result;
        Settled = true;
        SettledAt = DateTime.UtcNow;

        // Calculate PnL
        // For YES/UP: win if settlement == 1.0
        // For NO/DOWN: win if settlement == 0.0
        var direction = Side == OrderSide.Buy ? 1 : -1;  // simplified

        if (result == "win")
            Pnl = PnL.Create(Size * (1m - EntryPrice));
        else if (result == "loss")
            Pnl = PnL.Create(-Size * EntryPrice);
        else
            Pnl = PnL.Zero;

        UpdatedAt = DateTime.UtcNow;
    }

    public void TriggerStopLoss()
    {
        Status = TradeStatus.StopLossTriggered;
        Result = "stop_loss";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close() => Status = TradeStatus.Closed;

    public bool IsOpen => Status == TradeStatus.Open;
    public bool IsSettled => Settled;
    public bool IsWin => Result == "win";
    public bool IsLoss => Result == "loss";
}
