using FluentAssertions;
using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Services;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Application.Tests;

public class StopLossTests
{
    [Fact]
    public void Position_ShouldTriggerStopLoss_WhenPriceAtStopLoss()
    {
        // Create position at 0.99 with 20% stop → stop at 0.792
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        // Current price at stop loss level
        position.UpdateCurrentPrice(0.792m);
        position.ShouldTriggerStopLoss().Should().BeTrue();
    }

    [Fact]
    public void Position_ShouldTriggerStopLoss_WhenPriceBelowStopLoss()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        position.UpdateCurrentPrice(0.70m);
        position.ShouldTriggerStopLoss().Should().BeTrue();
    }

    [Fact]
    public void Position_ShouldNotTriggerStopLoss_WhenPriceAboveStopLoss()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        position.UpdateCurrentPrice(0.90m);
        position.ShouldTriggerStopLoss().Should().BeFalse();
    }

    [Fact]
    public async Task StopLossMonitor_CheckStopLoss_ReturnsTriggered()
    {
        var executor = new PaperStopLossExitExecutor();
        var monitor = new StopLossMonitor(executor, 0.20m);

        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        // Set position's current price to match the check
        position.UpdateCurrentPrice(0.792m);
        var result = await monitor.CheckStopLossAsync(position, 0.792m);

        result.PositionFound.Should().BeTrue();
        result.ShouldTrigger.Should().BeTrue();
        result.CurrentPrice.Should().Be(0.792m);
        result.StopLossPrice.Should().Be(0.792m);
    }

    [Fact]
    public async Task StopLossMonitor_TriggerStopLoss_ClosesPosition()
    {
        var executor = new PaperStopLossExitExecutor();
        var monitor = new StopLossMonitor(executor, 0.20m);

        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        var exitResult = await monitor.TriggerStopLossAsync(
            position,
            exitPrice: 0.792m,
            slippage: 0.001m,
            decisionTraceId: Guid.NewGuid());

        exitResult.Success.Should().BeTrue();
        exitResult.ExitPrice.Should().Be(0.792m);
        exitResult.Pnl.Should().NotBeNull();
        position.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Position_ClosedPosition_ShouldNotTriggerStopLoss()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        position.Close(PositionExitReason.Manual, PnL.Create(1m));

        position.UpdateCurrentPrice(0.70m);
        position.ShouldTriggerStopLoss().Should().BeFalse();
    }

    [Fact]
    public void Position_StopLossPrice_CalculatedCorrectly()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        // 0.99 * (1 - 0.20) = 0.792
        position.StopLossPrice.Should().Be(0.792m);
    }

    [Fact]
    public void Position_StopLossPrice_DifferentThresholds()
    {
        // Tight stop: 10%
        var tightPosition = Position.Create(
            Guid.NewGuid(), "test", OrderSide.Buy, 0.99m, 10m, 9.90m,
            StopLossThreshold.Tight);
        tightPosition.StopLossPrice.Should().Be(0.891m);

        // Wide stop: 30%
        var widePosition = Position.Create(
            Guid.NewGuid(), "test", OrderSide.Buy, 0.99m, 10m, 9.90m,
            StopLossThreshold.Wide);
        widePosition.StopLossPrice.Should().Be(0.693m);
    }
}
