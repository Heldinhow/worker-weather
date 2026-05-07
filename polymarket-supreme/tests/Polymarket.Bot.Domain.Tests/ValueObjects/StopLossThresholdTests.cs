using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Tests.ValueObjects;

public class StopLossThresholdTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.10)]
    [InlineData(0.20)]
    [InlineData(0.50)]
    [InlineData(1.0)]
    public void Create_WithValidValue_ReturnsThreshold(decimal validValue)
    {
        var threshold = StopLossThreshold.Create(validValue);
        Assert.Equal(validValue, threshold.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0)]
    [InlineData(1.01)]
    [InlineData(2)]
    public void Create_WithInvalidValue_Throws(decimal invalidValue)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => StopLossThreshold.Create(invalidValue));
        Assert.Contains("StopLossThreshold must be > 0 and <= 1", ex.Message);
    }

    [Fact]
    public void Default_IsTwentyPercent()
    {
        var threshold = StopLossThreshold.Default;
        Assert.Equal(0.20m, threshold.Value);
    }

    [Fact]
    public void CalculateStopPrice_ForEntryPoint99_ReturnsCorrectStop()
    {
        // Entry 0.99 with 20% stop = exit at ~0.792
        var threshold = StopLossThreshold.Create(0.20m);
        var stopPrice = threshold.CalculateStopPrice(0.99m);
        Assert.Equal(0.792m, stopPrice);
    }

    [Fact]
    public void CalculateStopPrice_ForEntryPoint50_ReturnsCorrectStop()
    {
        // Entry 0.50 with 20% stop = exit at 0.40
        var threshold = StopLossThreshold.Create(0.20m);
        var stopPrice = threshold.CalculateStopPrice(0.50m);
        Assert.Equal(0.40m, stopPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void CalculateStopPrice_WithInvalidEntryPrice_Throws(decimal invalidEntryPrice)
    {
        var threshold = StopLossThreshold.Default;
        Assert.Throws<ArgumentOutOfRangeException>(() => threshold.CalculateStopPrice(invalidEntryPrice));
    }

    [Fact]
    public void CalculateStopPrice_WithTightStop_CalculatesCorrectly()
    {
        var threshold = StopLossThreshold.Tight; // 10%
        var stopPrice = threshold.CalculateStopPrice(0.99m);
        Assert.Equal(0.891m, stopPrice);
    }

    [Fact]
    public void ToPercent_ReturnsCorrectPercentage()
    {
        var threshold = StopLossThreshold.Create(0.20m);
        Assert.Equal(20m, threshold.ToPercent);
    }

    [Fact]
    public void Static_Constants_AreCorrect()
    {
        Assert.Equal(0.10m, StopLossThreshold.Tight.Value);
        Assert.Equal(0.20m, StopLossThreshold.Moderate.Value);
        Assert.Equal(0.30m, StopLossThreshold.Wide.Value);
    }

    [Fact]
    public void Operators_Comparison_Works()
    {
        var tight = StopLossThreshold.Tight;
        var wide = StopLossThreshold.Wide;
        Assert.True(tight < wide);
        Assert.True(wide > tight);
        Assert.True(tight != wide);
    }

    [Fact]
    public void Equality_SameValueInstances_AreEqual()
    {
        var a = StopLossThreshold.Create(0.20m);
        var b = StopLossThreshold.Create(0.20m);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
