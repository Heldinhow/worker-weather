using FluentAssertions;
using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Services;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Application.Tests;

public class PaperTradingPipelineTests
{
    private readonly PaperOrderExecutor _executor;
    private readonly OrderFillSimulator _simulator;
    private readonly RiskManager _riskManager;
    private readonly TradeExecutionPipeline _pipeline;

    public PaperTradingPipelineTests()
    {
        _executor = new PaperOrderExecutor();
        _simulator = new OrderFillSimulator();
        _riskManager = new RiskManager();
        _pipeline = new TradeExecutionPipeline(_executor, _simulator, _riskManager, enablePaperTrading: true);
    }

    [Fact]
    public void PaperExecutor_Mode_IsPaper()
    {
        _executor.Mode.Should().Be(TradingMode.Paper);
    }

    [Fact]
    public async Task PaperExecutor_PlaceOrder_ReturnsSuccess()
    {
        var result = await _executor.PlaceOrderAsync(
            marketId: "test-market",
            outcomeId: "test-outcome",
            side: OrderSide.Buy,
            price: 0.99m,
            shares: 10m);

        result.Success.Should().BeTrue();
        result.OrderId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FillSimulator_AcceptableSpread_Passes()
    {
        var result = _simulator.IsSpreadAcceptable(
            bestBid: 0.989m,
            bestAsk: 0.991m,
            maxSpreadBps: 100m);

        result.Should().BeTrue();
    }

    [Fact]
    public void FillSimulator_WideSpread_Fails()
    {
        var result = _simulator.IsSpreadAcceptable(
            bestBid: 0.95m,
            bestAsk: 0.99m, // 4% spread
            maxSpreadBps: 100m); // 1% max

        result.Should().BeFalse();
    }

    [Fact]
    public void FillSimulator_SufficientLiquidity_Passes()
    {
        var result = _simulator.HasSufficientLiquidity(
            bestBid: 0.99m,
            bestAsk: 0.991m,
            requestedShares: 10m,
            minLiquidity: 25m);

        result.Should().BeTrue();
    }

    [Fact]
    public void FillSimulator_LowLiquidity_Fails()
    {
        var result = _simulator.HasSufficientLiquidity(
            bestBid: 0.99m,
            bestAsk: 0.991m,
            requestedShares: 1000m, // large order
            minLiquidity: 25m);

        result.Should().BeFalse();
    }

    [Fact]
    public void FillSimulator_SimulateFill_Success()
    {
        var result = _simulator.SimulateFill(
            requestedPrice: 0.99m,
            requestedShares: 10m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 1000m);

        result.Success.Should().BeTrue();
        result.Filled.Should().BeTrue();
        result.FilledPrice.Should().BeGreaterThan(0.99m); // Slippage added
        result.FilledShares.Should().Be(10m);
        result.Notional.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FillSimulator_InsufficientLiquidity_Rejects()
    {
        var result = _simulator.SimulateFill(
            requestedPrice: 0.99m,
            requestedShares: 10000m, // huge order
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 10m); // very low liquidity

        result.Success.Should().BeFalse();
        result.RejectReason.Should().Contain("liquidity");
    }

    [Fact]
    public void FillSimulator_ExcessiveSlippage_Rejects()
    {
        // Very large order relative to liquidity: 2000 shares at $0.99 = $1980 notional
        // With $100 liquidity, this consumes 19.8x the available liquidity
        // This creates excessive slippage that exceeds the 5 bps threshold
        var result = _simulator.SimulateFill(
            requestedPrice: 0.99m,
            requestedShares: 2000m,
            bestBid: 0.98m,
            bestAsk: 0.99m,
            liquidity: 100m,  // very low liquidity
            maxSlippageBps: 5m,  // tight max 5 bps
            feeRateBps: 10m);

        // Fill should fail because slippage is excessive
        result.Success.Should().BeFalse();
        result.Filled.Should().BeFalse();
        result.RejectReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FillSimulator_ModerateSlippage_Accepts()
    {
        // Moderate order relative to liquidity: 100 shares at $0.99 = $99 notional
        // With $500 liquidity, slippage should be acceptable
        var result = _simulator.SimulateFill(
            requestedPrice: 0.99m,
            requestedShares: 100m,
            bestBid: 0.98m,
            bestAsk: 0.99m,
            liquidity: 500m,
            maxSlippageBps: 10m);

        result.Success.Should().BeTrue();
        result.Filled.Should().BeTrue();
        result.FilledPrice.Should().BeGreaterThan(0.99m);
    }

    [Fact]
    public async Task Pipeline_PaperTrade_ExecutesSuccessfully()
    {
        var request = new TradeExecutionRequest(
            ExecutionCycleId: Guid.NewGuid(),
            DecisionTraceId: Guid.NewGuid(),
            MarketId: "btc-updown-5m-test",
            MarketSlug: "btc-updown-5m-test",
            OutcomeId: "test-outcome-up",
            Side: OrderSide.Buy,
            EntryPrice: 0.99m,
            BestBid: 0.989m,
            BestAsk: 0.991m,
            Liquidity: 1000m,
            Size: 9.90m,
            TimeToResolutionSeconds: 300);

        var result = await _pipeline.ExecuteAsync(request);

        result.Success.Should().BeTrue();
        result.Executed.Should().BeTrue();
        result.OrderId.Should().NotBeNull();
        result.EntryPrice.Should().BeGreaterThan(0);
        result.Shares.Should().BeGreaterThan(0);
        result.Notional.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Pipeline_LowLiquidity_Rejects()
    {
        var request = new TradeExecutionRequest(
            ExecutionCycleId: Guid.NewGuid(),
            DecisionTraceId: Guid.NewGuid(),
            MarketId: "btc-updown-5m-test",
            MarketSlug: "btc-updown-5m-test",
            OutcomeId: "test-outcome-up",
            Side: OrderSide.Buy,
            EntryPrice: 0.99m,
            BestBid: 0.989m,
            BestAsk: 0.991m,
            Liquidity: 1m, // very low liquidity
            Size: 9.90m);

        var result = await _pipeline.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Executed.Should().BeFalse();
    }

    [Fact]
    public async Task Pipeline_WideSpread_Rejects()
    {
        var request = new TradeExecutionRequest(
            ExecutionCycleId: Guid.NewGuid(),
            DecisionTraceId: Guid.NewGuid(),
            MarketId: "btc-updown-5m-test",
            MarketSlug: "btc-updown-5m-test",
            OutcomeId: "test-outcome-up",
            Side: OrderSide.Buy,
            EntryPrice: 0.50m,
            BestBid: 0.45m, // wide spread
            BestAsk: 0.55m,
            Liquidity: 1000m,
            Size: 5m);

        var result = await _pipeline.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.RejectReason.Should().Contain("Spread");
    }

    [Fact]
    public async Task Pipeline_PaperDisabled_Rejects()
    {
        var disabledPipeline = new TradeExecutionPipeline(
            _executor, _simulator, _riskManager, enablePaperTrading: false);

        var request = new TradeExecutionRequest(
            ExecutionCycleId: Guid.NewGuid(),
            DecisionTraceId: Guid.NewGuid(),
            MarketId: "test",
            MarketSlug: "test",
            OutcomeId: "outcome",
            Side: OrderSide.Buy,
            EntryPrice: 0.99m,
            BestBid: 0.989m,
            BestAsk: 0.991m,
            Liquidity: 1000m,
            Size: 9.90m);

        var result = await disabledPipeline.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.RejectReason.Should().Contain("Paper trading is disabled");
    }
}
