using PolymarketBot.Framework.Domain.ValueObjects;

public class Position
{
    public Guid Id { get; set; }
    public string MarketId { get; set; } = string.Empty;
    public Direction Direction { get; set; }
    public Price EntryPrice { get; set; } = null!;
    public Price CurrentPrice { get; set; } = null!;
    public decimal Size { get; set; }
    public decimal StopLossPercent { get; set; } = 0.20m;
    public decimal TakeProfitPercent { get; set; } = 0.80m;
    public bool TrailingStopEnabled { get; set; } = true;
    public decimal TrailingActivationPercent { get; set; } = 0.20m;
    public PositionStatus Status { get; set; } = PositionStatus.Open;
    public DateTime OpenedAt { get; set; }
}

public enum Direction { YES, NO }
public enum PositionStatus { Open, Closed, StopLoss, TakeProfit }
