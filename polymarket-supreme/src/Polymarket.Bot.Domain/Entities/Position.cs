using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents an open or closed position.
/// </summary>
public class Position : Entity
{
    public Guid MarketId { get; private set; }
    public string MarketTicker { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public decimal EntryPrice { get; private set; }
    public StopLossThreshold StopLossThreshold { get; private set; } = StopLossThreshold.Default;
    public decimal StopLossPrice { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public decimal Shares { get; private set; }
    public decimal Size { get; private set; }  // Notional $
    public PositionStatus Status { get; private set; } = PositionStatus.Open;
    public PnL RealizedPnl { get; private set; } = PnL.Zero;
    public PnL UnrealizedPnl { get; private set; } = PnL.Zero;
    public DateTime OpenedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }
    public PositionExitReason? ExitReason { get; private set; }
    public string? MetadataJson { get; private set; }

    private readonly List<Trade> _trades = new();
    public IReadOnlyList<Trade> Trades => _trades.AsReadOnly();

    private Position() { }

    public static Position Create(
        Guid marketId,
        string marketTicker,
        OrderSide side,
        decimal entryPrice,
        decimal shares,
        decimal size,
        StopLossThreshold? stopLossThreshold = null)
    {
        if (entryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(entryPrice), "Entry price must be positive");
        if (shares <= 0) throw new ArgumentOutOfRangeException(nameof(shares), "Shares must be positive");
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive");

        var threshold = stopLossThreshold ?? StopLossThreshold.Default;
        var stopPrice = threshold.CalculateStopPrice(entryPrice);

        return new Position
        {
            MarketId = marketId,
            MarketTicker = marketTicker,
            Side = side,
            EntryPrice = entryPrice,
            StopLossThreshold = threshold,
            StopLossPrice = stopPrice,
            CurrentPrice = entryPrice,
            Shares = shares,
            Size = size,
            OpenedAt = DateTime.UtcNow
        };
    }

    public void UpdateCurrentPrice(decimal price)
    {
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        CurrentPrice = price;
        UpdateUnrealizedPnl();
    }

    private void UpdateUnrealizedPnl()
    {
        if (Side == OrderSide.Buy)
        {
            // For YES/UP position: profit if current > entry
            UnrealizedPnl = PnL.Create((CurrentPrice - EntryPrice) * Shares);
        }
        else
        {
            // For NO/DOWN position: profit if current < entry
            UnrealizedPnl = PnL.Create((EntryPrice - CurrentPrice) * Shares);
        }
    }

    public void AddTrade(Trade trade)
    {
        if (trade is null) throw new ArgumentNullException(nameof(trade));
        _trades.Add(trade);
    }

    public bool ShouldTriggerStopLoss()
    {
        if (Status != PositionStatus.Open) return false;
        if (CurrentPrice <= 0) return false;
        // BUY (YES): trigger when price drops to/below stop loss
        // SELL (NO): trigger when price rises to/above stop loss
        return Side == OrderSide.Buy
            ? CurrentPrice <= StopLossPrice
            : CurrentPrice >= StopLossPrice;
    }

    public void Close(PositionExitReason reason, PnL realizedPnl)
    {
        Status = reason == PositionExitReason.StopLoss ? PositionStatus.StopLossTriggered : PositionStatus.Closed;
        ExitReason = reason;
        RealizedPnl = realizedPnl;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddToPosition(decimal additionalShares, decimal additionalSize, decimal newEntryPrice)
    {
        if (additionalShares <= 0) throw new ArgumentOutOfRangeException(nameof(additionalShares));
        if (additionalSize <= 0) throw new ArgumentOutOfRangeException(nameof(additionalSize));
        if (newEntryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(newEntryPrice));

        // Weighted average entry price
        var totalShares = Shares + additionalShares;
        var totalCost = (EntryPrice * Shares) + (newEntryPrice * additionalShares);
        EntryPrice = Math.Round(totalCost / totalShares, 6);
        Shares = Math.Round(totalShares, 4);
        Size = Math.Round(Size + additionalSize, 2);
        StopLossPrice = StopLossThreshold.CalculateStopPrice(EntryPrice);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOpen => Status == PositionStatus.Open;
    public bool IsClosed => Status != PositionStatus.Open;
    public decimal TotalPnl => RealizedPnl.Value + UnrealizedPnl.Value;
}
