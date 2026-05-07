using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Tests.Entities;

public class PositionTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesOpenPosition()
    {
        var marketId = Guid.NewGuid();
        var position = Position.Create(
            marketId,
            "btc-updown-5m-123456",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m);

        Assert.True(position.IsOpen);
        Assert.Equal(PositionStatus.Open, position.Status);
        Assert.Equal(0.99m, position.EntryPrice);
        Assert.Equal(10m, position.Shares);
        Assert.Equal(9.90m, position.Size);
        Assert.NotEqual(Guid.Empty, position.Id);
    }

    [Fact]
    public void Create_CalculatesStopLossPrice_Correctly()
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
        Assert.Equal(0.792m, position.StopLossPrice);
    }

    [Fact]
    public void Create_WithNullStopLoss_UsesDefault()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.50m,
            shares: 20m,
            size: 10m);

        Assert.Equal(StopLossThreshold.Default.Value, position.StopLossThreshold.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidEntryPrice_Throws(decimal invalidPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Position.Create(
                Guid.NewGuid(),
                "test-market",
                OrderSide.Buy,
                entryPrice: invalidPrice,
                shares: 10m,
                size: 1m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Create_WithInvalidShares_Throws(decimal invalidShares)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Position.Create(
                Guid.NewGuid(),
                "test-market",
                OrderSide.Buy,
                entryPrice: 0.50m,
                shares: invalidShares,
                size: 1m));
    }

    [Fact]
    public void Create_RequiresStopLossPrice_ButNotExplicitly()
    {
        // The key domain rule: Position should never exist without StopLossPrice
        // Our factory ensures this by requiring stop loss or using default
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m);

        // StopLossPrice MUST be set (computed from threshold)
        Assert.True(position.StopLossPrice > 0);
        Assert.True(position.StopLossPrice < position.EntryPrice);
    }

    [Fact]
    public void ShouldTriggerStopLoss_WhenPriceAtOrBelowStopLoss()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        // Price dropped to stop loss level (0.792)
        position.UpdateCurrentPrice(0.792m);
        Assert.True(position.ShouldTriggerStopLoss());

        // Price dropped below stop loss
        position.UpdateCurrentPrice(0.75m);
        Assert.True(position.ShouldTriggerStopLoss());
    }

    [Fact]
    public void ShouldTriggerStopLoss_WhenPriceAboveStopLoss()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m,
            stopLossThreshold: StopLossThreshold.Create(0.20m));

        // Price still above stop loss
        position.UpdateCurrentPrice(0.90m);
        Assert.False(position.ShouldTriggerStopLoss());
    }

    [Fact]
    public void Close_WithStopLossReason_SetsCorrectStatus()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m);

        var realizedPnl = PnL.Create(-1.50m); // Loss from stop loss exit
        position.Close(PositionExitReason.StopLoss, realizedPnl);

        Assert.Equal(PositionStatus.StopLossTriggered, position.Status);
        Assert.False(position.IsOpen);
        Assert.Equal(PositionExitReason.StopLoss, position.ExitReason);
        Assert.NotNull(position.ClosedAt);
    }

    [Fact]
    public void Close_WithManualReason_SetsCorrectStatus()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.99m,
            shares: 10m,
            size: 9.90m);

        var realizedPnl = PnL.Create(2.50m); // Profit
        position.Close(PositionExitReason.Manual, realizedPnl);

        Assert.Equal(PositionStatus.Closed, position.Status);
        Assert.Equal(PositionExitReason.Manual, position.ExitReason);
    }

    [Fact]
    public void UpdateUnrealizedPnl_CalculatesCorrectly_ForBuyPosition()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.50m,
            shares: 10m,
            size: 5m);

        // Price went up to 0.60
        position.UpdateCurrentPrice(0.60m);
        // PnL = (0.60 - 0.50) * 10 = 1.00
        Assert.Equal(1.00m, position.UnrealizedPnl.Value);
        Assert.True(position.UnrealizedPnl.IsProfit);
    }

    [Fact]
    public void UpdateUnrealizedPnl_CalculatesCorrectly_ForSellPosition()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Sell,
            entryPrice: 0.50m,
            shares: 10m,
            size: 5m);

        // Price went down to 0.40 (we bet NO, so profit if outcome resolves NO)
        position.UpdateCurrentPrice(0.40m);
        // PnL = (0.50 - 0.40) * 10 = 1.00 profit
        Assert.Equal(1.00m, position.UnrealizedPnl.Value);
        Assert.True(position.UnrealizedPnl.IsProfit);
    }

    [Fact]
    public void AddToPosition_UpdatesEntryPrice()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.50m,
            shares: 10m,
            size: 5m);

        // Add more to position at 0.60
        position.AddToPosition(
            additionalShares: 5m,
            additionalSize: 3m,
            newEntryPrice: 0.60m);

        // Total: 15 shares, cost basis updated
        Assert.True(position.Shares > 10m);
        Assert.True(position.EntryPrice > 0.50m);
        Assert.True(position.StopLossPrice < 0.50m); // Stop loss adjusted
    }

    [Fact]
    public void TotalPnl_CombinesRealizedAndUnrealized()
    {
        var position = Position.Create(
            Guid.NewGuid(),
            "test-market",
            OrderSide.Buy,
            entryPrice: 0.50m,
            shares: 10m,
            size: 5m);

        position.UpdateCurrentPrice(0.60m);

        var closedPnl = PnL.Create(0.50m);
        position.Close(PositionExitReason.Manual, closedPnl);

        Assert.Equal(1.50m, position.TotalPnl); // 0.50 realized + 1.00 unrealized
    }
}
