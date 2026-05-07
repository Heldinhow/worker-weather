using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Services;

/// <summary>
/// Domain service for stop loss operations.
/// </summary>
public static class PositionStopLossService
{
    /// <summary>
    /// Check if a position should trigger stop loss based on current price.
    /// </summary>
    public static bool ShouldTriggerStopLoss(Position position, decimal currentPrice)
    {
        if (position is null) throw new ArgumentNullException(nameof(position));
        if (currentPrice < 0) throw new ArgumentOutOfRangeException(nameof(currentPrice));
        if (!position.IsOpen) return false;

        return currentPrice <= position.StopLossPrice;
    }

    /// <summary>
    /// Calculate the current loss percentage for a position.
    /// </summary>
    public static decimal CalculateLossPercentage(Position position, decimal currentPrice)
    {
        if (position is null) throw new ArgumentNullException(nameof(position));
        if (currentPrice <= 0) return 0m;

        var entryPrice = position.EntryPrice;
        if (entryPrice <= 0) return 0m;

        if (position.Side == OrderSide.Buy)
        {
            // For YES position: loss = (entry - current) / entry
            return Math.Max(0, (entryPrice - currentPrice) / entryPrice);
        }
        else
        {
            // For NO position: loss = (current - entry) / entry
            return Math.Max(0, (currentPrice - entryPrice) / entryPrice);
        }
    }

    /// <summary>
    /// Calculate the distance to stop loss price as percentage.
    /// </summary>
    public static decimal CalculateDistanceToStopLoss(Position position, decimal currentPrice)
    {
        if (position is null) throw new ArgumentNullException(nameof(position));
        if (currentPrice <= 0) return 0m;

        var entryPrice = position.EntryPrice;
        if (entryPrice <= 0) return 0m;

        if (position.Side == OrderSide.Buy)
        {
            // Distance = (current - stop) / entry
            return Math.Max(0, (currentPrice - position.StopLossPrice) / entryPrice);
        }
        else
        {
            // Distance = (stop - current) / entry
            return Math.Max(0, (position.StopLossPrice - currentPrice) / entryPrice);
        }
    }

    /// <summary>
    /// Calculate the price at which stop loss is triggered.
    /// </summary>
    public static decimal CalculateStopLossTriggerPrice(Position position)
    {
        if (position is null) throw new ArgumentNullException(nameof(position));
        return position.StopLossPrice;
    }

    /// <summary>
    /// Determine the best exit price based on market conditions.
    /// </summary>
    public static decimal DetermineExitPrice(
        decimal bestBid,
        decimal stopLossPrice,
        decimal maxSlippage = 0.005m)
    {
        if (bestBid < 0) throw new ArgumentOutOfRangeException(nameof(bestBid));
        if (stopLossPrice < 0) throw new ArgumentOutOfRangeException(nameof(stopLossPrice));

        // Exit at best bid if it's above stop loss (adjusted for slippage)
        var minAcceptablePrice = stopLossPrice * (1m - maxSlippage);
        return Math.Max(bestBid, minAcceptablePrice);
    }
}
