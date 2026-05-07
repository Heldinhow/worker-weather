using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Application.Services;

/// <summary>
/// Stop loss monitor service.
/// </summary>
public class StopLossMonitor : IStopLossMonitor
{
    private readonly IStopLossExitExecutor _exitExecutor;
    private readonly decimal _stopLossPercentage;

    public StopLossMonitor(
        IStopLossExitExecutor exitExecutor,
        decimal stopLossPercentage = 0.20m)
    {
        _exitExecutor = exitExecutor ?? throw new ArgumentNullException(nameof(exitExecutor));
        _stopLossPercentage = stopLossPercentage;
    }

    public Task<StopLossCheckResult> CheckStopLossAsync(
        Position position,
        decimal currentPrice,
        CancellationToken ct = default)
    {
        if (position is null)
            return Task.FromResult(new StopLossCheckResult(
                ShouldTrigger: false,
                PositionFound: false));

        var shouldTrigger = position.ShouldTriggerStopLoss()
            && currentPrice <= position.StopLossPrice;

        var lossPercentage = CalculateLossPercentage(position, currentPrice);

        return Task.FromResult(new StopLossCheckResult(
            ShouldTrigger: shouldTrigger,
            PositionFound: true,
            CurrentPrice: currentPrice,
            StopLossPrice: position.StopLossPrice,
            LossPercentage: lossPercentage));
    }

    public async Task<StopLossCheckResult> CheckAllStopLossesAsync(
        IEnumerable<Position> positions,
        Func<Position, decimal> getCurrentPrice,
        CancellationToken ct = default)
    {
        var positionList = positions.ToList();
        if (!positionList.Any())
            return new StopLossCheckResult(
                ShouldTrigger: false,
                PositionFound: false);

        // Check each position
        foreach (var position in positionList)
        {
            var currentPrice = getCurrentPrice(position);
            var result = await CheckStopLossAsync(position, currentPrice, ct);

            if (result.ShouldTrigger)
            {
                return result; // Return first triggered stop loss
            }
        }

        return new StopLossCheckResult(
            ShouldTrigger: false,
            PositionFound: true);
    }

    public async Task<StopLossExitResult> TriggerStopLossAsync(
        Position position,
        decimal exitPrice,
        decimal slippage,
        Guid decisionTraceId,
        CancellationToken ct = default)
    {
        return await _exitExecutor.ExitPositionAsync(
            position,
            exitPrice,
            slippage,
            decisionTraceId,
            ct);
    }

    private static decimal CalculateLossPercentage(Position position, decimal currentPrice)
    {
        if (position.EntryPrice <= 0)
            return 0m;

        if (position.Side == OrderSide.Buy)
        {
            return Math.Max(0, (position.EntryPrice - currentPrice) / position.EntryPrice);
        }
        else
        {
            return Math.Max(0, (currentPrice - position.EntryPrice) / position.EntryPrice);
        }
    }
}

/// <summary>
/// Exit executor for stop loss positions.
/// </summary>
public interface IStopLossExitExecutor
{
    Task<StopLossExitResult> ExitPositionAsync(
        Position position,
        decimal exitPrice,
        decimal slippage,
        Guid decisionTraceId,
        CancellationToken ct = default);
}

/// <summary>
/// Paper stop loss exit executor (simulated).
/// </summary>
public class PaperStopLossExitExecutor : IStopLossExitExecutor
{
    public async Task<StopLossExitResult> ExitPositionAsync(
        Position position,
        decimal exitPrice,
        decimal slippage,
        Guid decisionTraceId,
        CancellationToken ct = default)
    {
        // Simulate async operation
        await Task.Delay(1, ct);

        if (!position.IsOpen)
        {
            return new StopLossExitResult(
                Success: false,
                RejectReason: "Position is already closed");
        }

        // Calculate P&L
        var shares = position.Shares;
        var entryNotional = position.EntryPrice * shares;
        var exitNotional = exitPrice * shares;

        var pnl = position.Side == OrderSide.Buy
            ? PnL.Create(exitNotional - entryNotional)
            : PnL.Create(entryNotional - exitNotional);

        // Close position
        position.Close(PositionExitReason.StopLoss, pnl);

        return new StopLossExitResult(
            Success: true,
            ExitPrice: exitPrice,
            Shares: shares,
            Notional: exitNotional,
            Pnl: new PnLResult(pnl.Value, 0, pnl.Value));
    }
}
