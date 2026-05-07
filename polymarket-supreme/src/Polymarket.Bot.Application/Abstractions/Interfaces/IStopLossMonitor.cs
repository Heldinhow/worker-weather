using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Monitors positions and triggers stop losses.
/// </summary>
public interface IStopLossMonitor
{
    /// <summary>
    /// Check stop loss for a single position.
    /// </summary>
    Task<StopLossCheckResult> CheckStopLossAsync(
        Position position,
        decimal currentPrice,
        CancellationToken ct = default);

    /// <summary>
    /// Check stop losses for all open positions.
    /// </summary>
    Task<StopLossCheckResult> CheckAllStopLossesAsync(
        IEnumerable<Position> positions,
        Func<Position, decimal> getCurrentPrice,
        CancellationToken ct = default);

    /// <summary>
    /// Trigger a stop loss exit for a position.
    /// </summary>
    Task<StopLossExitResult> TriggerStopLossAsync(
        Position position,
        decimal exitPrice,
        decimal slippage,
        Guid decisionTraceId,
        CancellationToken ct = default);
}
