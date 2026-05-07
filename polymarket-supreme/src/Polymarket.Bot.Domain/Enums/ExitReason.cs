namespace Polymarket.Bot.Domain.Enums;

public enum PositionExitReason
{
    Manual,
    StopLoss,
    TakeProfit,
    MarketSettled,
    TimeExpired
}
