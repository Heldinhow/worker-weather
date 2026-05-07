using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Interface for order execution (paper or live).
/// </summary>
public interface IOrderExecutor
{
    TradingMode Mode { get; }

    Task<OrderPlacementResult> PlaceOrderAsync(
        string marketId,
        string outcomeId,
        OrderSide side,
        decimal price,
        decimal shares,
        CancellationToken ct = default);

    Task<OrderCancellationResult> CancelOrderAsync(
        string orderId,
        CancellationToken ct = default);
}

/// <summary>
/// Paper trading order executor (simulated fills).
/// </summary>
public interface IPaperOrderExecutor : IOrderExecutor
{
}

/// <summary>
/// Live trading order executor (real Polymarket API).
/// </summary>
public interface ILiveOrderExecutor : IOrderExecutor
{
    bool IsConnected { get; }

    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}
