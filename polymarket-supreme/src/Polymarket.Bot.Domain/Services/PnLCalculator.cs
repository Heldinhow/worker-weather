using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Services;

/// <summary>
/// Domain service for calculating P&L of trades and positions.
/// </summary>
public static class PnLCalculator
{
    /// <summary>
    /// Calculate realized P&L for a trade given settlement value.
    /// SettlementValue: 1.0 = YES/UP won, 0.0 = NO/DOWN won
    /// </summary>
    public static PnL CalculateTradePnL(Trade trade, decimal settlementValue)
    {
        if (trade is null) throw new ArgumentNullException(nameof(trade));
        if (settlementValue < 0 || settlementValue > 1)
            throw new ArgumentOutOfRangeException(nameof(settlementValue));

        // For YES/UP position: win if settlement == 1.0
        // For NO/DOWN position: win if settlement == 0.0
        bool isYesPosition = trade.Side == OrderSide.Buy;
        bool won;

        if (isYesPosition)
            won = settlementValue >= 0.9m;
        else
            won = settlementValue <= 0.1m;

        if (won)
        {
            // Profit: (1 - entry_price) * shares = (1 - p) * notional / p
            // Simplified: size * (1/p - 1) = size * (1-p)/p
            var profit = trade.Size * ((1m - trade.EntryPrice) / trade.EntryPrice);
            return PnL.Create(profit);
        }
        else
        {
            // Loss: entry_price * shares / entry_price = size
            return PnL.Create(-trade.Size);
        }
    }

    /// <summary>
    /// Calculate unrealized P&L for a position at current price.
    /// </summary>
    public static PnL CalculateUnrealizedPnL(Position position)
    {
        if (position is null) throw new ArgumentNullException(nameof(position));

        if (position.Side == OrderSide.Buy)
        {
            // YES/UP position: profit if current > entry
            var pnl = (position.CurrentPrice - position.EntryPrice) * position.Shares;
            return PnL.Create(pnl);
        }
        else
        {
            // NO/DOWN position: profit if current < entry
            var pnl = (position.EntryPrice - position.CurrentPrice) * position.Shares;
            return PnL.Create(pnl);
        }
    }

    /// <summary>
    /// Calculate stop loss P&L for a position.
    /// </summary>
    public static PnL CalculateStopLossPnL(Position position)
    {
        if (position is null) throw new ArgumentNullException(nameof(position));

        // Exit at stop loss price
        var exitNotional = position.StopLossPrice * position.Shares;
        var entryNotional = position.EntryPrice * position.Shares;

        if (position.Side == OrderSide.Buy)
            return PnL.Create(exitNotional - entryNotional);
        else
            return PnL.Create(entryNotional - exitNotional);
    }

    /// <summary>
    /// Calculate expected value for a trade given model probability.
    /// </summary>
    public static decimal CalculateExpectedValue(decimal modelProbability, decimal marketPrice)
    {
        if (modelProbability < 0 || modelProbability > 1)
            throw new ArgumentOutOfRangeException(nameof(modelProbability));
        if (marketPrice <= 0 || marketPrice >= 1)
            throw new ArgumentOutOfRangeException(nameof(marketPrice));

        var payout = 1m - marketPrice; // Profit if win
        var cost = marketPrice;       // Cost if lose
        var loseProbability = 1m - modelProbability;

        // EV = P_win * payout - P_lose * cost
        return modelProbability * payout - loseProbability * cost;
    }

    /// <summary>
    /// Calculate Kelly fraction for position sizing.
    /// </summary>
    public static decimal CalculateKellyFraction(decimal edge, decimal probability, decimal marketPrice)
    {
        if (probability <= 0 || probability >= 1)
            return 0m;
        if (marketPrice <= 0 || marketPrice >= 1)
            return 0m;

        var odds = (1m - marketPrice) / marketPrice;
        var loseProbability = 1m - probability;

        // Kelly = (p * odds - q) / odds
        var kelly = (probability * odds - loseProbability) / odds;

        // Fractional Kelly (cap at 25%)
        var fractionalKelly = Math.Max(0, Math.Min(kelly, 0.25m));

        return fractionalKelly;
    }
}
