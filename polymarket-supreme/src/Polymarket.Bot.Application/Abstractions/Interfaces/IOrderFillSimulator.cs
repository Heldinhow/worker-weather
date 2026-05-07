using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Simulates order fill based on market conditions.
/// </summary>
public interface IOrderFillSimulator
{
    /// <summary>
    /// Simulate a fill for a pending order.
    /// </summary>
    FillSimulationResult SimulateFill(
        decimal requestedPrice,
        decimal requestedShares,
        decimal bestBid,
        decimal bestAsk,
        decimal liquidity,
        decimal maxSlippageBps = 10m,
        decimal feeRateBps = 10m);

    /// <summary>
    /// Check if spread is acceptable for a fill.
    /// </summary>
    bool IsSpreadAcceptable(decimal bestBid, decimal bestAsk, decimal maxSpreadBps);

    /// <summary>
    /// Check if liquidity is sufficient for the order.
    /// </summary>
    bool HasSufficientLiquidity(decimal bestBid, decimal bestAsk, decimal requestedShares, decimal minLiquidity);
}

/// <summary>
/// Result of an order placement attempt.
/// </summary>
public record OrderPlacementResult(
    bool Success,
    string? OrderId = null,
    string? RejectReason = null,
    string? Error = null);

/// <summary>
/// Result of an order cancellation attempt.
/// </summary>
public record OrderCancellationResult(
    bool Success,
    string? RejectReason = null);
