using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Abstractions.Results;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Application.Services;

/// <summary>
/// Orchestrates paper trading execution pipeline.
/// </summary>
public class TradeExecutionPipeline : ITradeExecutionPipeline
{
    private readonly IPaperOrderExecutor _orderExecutor;
    private readonly IOrderFillSimulator _fillSimulator;
    private readonly RiskManager _riskManager;
    private readonly bool _enablePaperTrading;

    public TradeExecutionPipeline(
        IPaperOrderExecutor orderExecutor,
        IOrderFillSimulator fillSimulator,
        RiskManager riskManager,
        bool enablePaperTrading = true)
    {
        _orderExecutor = orderExecutor ?? throw new ArgumentNullException(nameof(orderExecutor));
        _fillSimulator = fillSimulator ?? throw new ArgumentNullException(nameof(fillSimulator));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
        _enablePaperTrading = enablePaperTrading;
    }

    public async Task<TradeExecutionResult> ExecuteAsync(
        TradeExecutionRequest request,
        CancellationToken ct = default)
    {
        // Step 1: Risk evaluation
        var riskResult = _riskManager.Evaluate(
            request.EntryPrice,
            request.BestBid,
            request.BestAsk,
            request.Liquidity,
            null,    // volume24h
            request.TimeToResolutionSeconds,
            null,     // dailyPnl
            0);       // openPositionsCount

        if (!riskResult.IsApproved)
        {
            return new TradeExecutionResult(
                false, false,
                null, null, null, null, null, null, null,
                null,
                $"Risk rejected: {riskResult.Reason}",
                _riskManager.GetAllRuleEvaluations(
                    request.EntryPrice, request.BestBid, request.BestAsk, request.Liquidity,
                    null, request.TimeToResolutionSeconds, null, 0));
        }

        // Step 2: Paper trading only
        if (!_enablePaperTrading)
        {
            return new TradeExecutionResult(false, false, null, null, null, null, null, null, null, null,
                "Paper trading is disabled");
        }

        // Step 3: Check spread acceptability
        if (!_fillSimulator.IsSpreadAcceptable(request.BestBid, request.BestAsk, 100m))
        {
            return new TradeExecutionResult(false, false, null, null, null, null, null, null, null, null,
                $"Spread too wide: bid={request.BestBid:P4} ask={request.BestAsk:P4}");
        }

        // Step 4: Simulate fill
        var shares = request.Size / request.EntryPrice;
        var fillResult = _fillSimulator.SimulateFill(
            request.EntryPrice,
            shares,
            request.BestBid,
            request.BestAsk,
            request.Liquidity,
            10m,   // maxSlippageBps
            10m);  // feeRateBps

        if (!fillResult.Success || !fillResult.Filled)
        {
            return new TradeExecutionResult(false, false, null, null, null, null, null, null, null, null,
                fillResult.RejectReason ?? "Fill simulation failed");
        }

        // Step 5: Place order (paper mode)
        var placementResult = await _orderExecutor.PlaceOrderAsync(
            request.MarketId,
            request.OutcomeId,
            request.Side,
            fillResult.FilledPrice,
            fillResult.FilledShares,
            ct);

        if (!placementResult.Success)
        {
            return new TradeExecutionResult(false, false, null, null, null, null, null, null, null, null,
                placementResult.RejectReason ?? "Order placement failed");
        }

        // Step 6: Calculate P&L
        var pnl = CalculatePnl(fillResult.FilledPrice, fillResult.FilledShares, request.Side);

        return new TradeExecutionResult(
            true, true,
            placementResult.OrderId is not null ? Guid.Parse(placementResult.OrderId) : null,
            null, null,
            fillResult.FilledPrice,
            fillResult.FilledShares,
            fillResult.Notional,
            fillResult.Slippage,
            new PnLResult(pnl.Value, 0, pnl.Value),
            null,
            _riskManager.GetAllRuleEvaluations(
                request.EntryPrice, request.BestBid, request.BestAsk, request.Liquidity,
                null, request.TimeToResolutionSeconds, null, 0));
    }

    private static PnL CalculatePnl(decimal entryPrice, decimal shares, OrderSide side)
    {
        return PnL.Zero;
    }
}

/// <summary>
/// Paper order executor (stub implementation).
/// </summary>
public class PaperOrderExecutor : IPaperOrderExecutor
{
    public TradingMode Mode => TradingMode.Paper;

    public Task<OrderPlacementResult> PlaceOrderAsync(
        string marketId,
        string outcomeId,
        OrderSide side,
        decimal price,
        decimal shares,
        CancellationToken ct = default)
    {
        var orderId = Guid.NewGuid().ToString();
        return Task.FromResult(new OrderPlacementResult(true, orderId));
    }

    public Task<OrderCancellationResult> CancelOrderAsync(string orderId, CancellationToken ct = default)
    {
        return Task.FromResult(new OrderCancellationResult(true));
    }
}

/// <summary>
/// Order fill simulator.
/// </summary>
public class OrderFillSimulator : IOrderFillSimulator
{
    public FillSimulationResult SimulateFill(
        decimal requestedPrice,
        decimal requestedShares,
        decimal bestBid,
        decimal bestAsk,
        decimal liquidity,
        decimal maxSlippageBps = 10m,
        decimal feeRateBps = 10m)
    {
        // Check liquidity
        var maxFillableShares = liquidity / requestedPrice;
        var liquidityRatio = requestedShares / (maxFillableShares > 0 ? maxFillableShares : 0.001m);
        
        // If order exceeds 10% of max fillable shares, reject for insufficient liquidity
        if (maxFillableShares < requestedShares * 0.1m)
        {
            return new FillSimulationResult(
                false, false, 0, 0, 0, 0, "Insufficient liquidity for fill");
        }

        // Calculate slippage based on liquidity ratio (orders consume more of the book)
        var liquidityFraction = Math.Min(1m, liquidityRatio / 10m);
        var slippageBps = liquidityFraction * maxSlippageBps;
        var slippageAmount = requestedPrice * (slippageBps / 10000m);

        // Fill at best ask + slippage
        var filledPrice = requestedPrice + slippageAmount;
        var filledShares = requestedShares;
        var actualSlippageBps = ((filledPrice - requestedPrice) / requestedPrice) * 10000m;

        if (actualSlippageBps > maxSlippageBps)
        {
            return new FillSimulationResult(
                true, false, filledPrice, 0, slippageAmount, 0,
                $"Slippage {actualSlippageBps:N1} bps exceeds max {maxSlippageBps:N1} bps");
        }

        var notional = filledPrice * filledShares;
        return new FillSimulationResult(
            true, true,
            Math.Round(filledPrice, 6),
            Math.Round(filledShares, 4),
            Math.Round(slippageAmount, 6),
            Math.Round(notional, 2));
    }

    public bool IsSpreadAcceptable(decimal bestBid, decimal bestAsk, decimal maxSpreadBps)
    {
        if (bestBid <= 0 || bestAsk <= 0 || bestBid > bestAsk)
            return false;

        var spread = bestAsk - bestBid;
        var midPrice = (bestBid + bestAsk) / 2;
        var spreadBps = midPrice > 0 ? (spread / midPrice) * 10000m : 0m;
        return spreadBps <= maxSpreadBps;
    }

    public bool HasSufficientLiquidity(
        decimal bestBid,
        decimal bestAsk,
        decimal requestedShares,
        decimal minLiquidity)
    {
        if (bestAsk <= 0)
            return false;

        var estimatedNotional = bestAsk * requestedShares;
        return estimatedNotional <= minLiquidity * 10;
    }
}
