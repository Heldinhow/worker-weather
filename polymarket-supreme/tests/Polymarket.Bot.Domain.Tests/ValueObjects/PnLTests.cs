using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Tests.ValueObjects;

public class PnLTests
{
    [Fact]
    public void Create_WithPositiveValue_CreatesProfit()
    {
        var pnl = PnL.Create(100m);
        Assert.Equal(100m, pnl.Value);
        Assert.True(pnl.IsProfit);
        Assert.False(pnl.IsLoss);
        Assert.False(pnl.IsBreakEven);
    }

    [Fact]
    public void Create_WithNegativeValue_CreatesLoss()
    {
        var pnl = PnL.Create(-50m);
        Assert.Equal(-50m, pnl.Value);
        Assert.False(pnl.IsProfit);
        Assert.True(pnl.IsLoss);
        Assert.False(pnl.IsBreakEven);
    }

    [Fact]
    public void Create_WithZero_CreatesBreakEven()
    {
        var pnl = PnL.Create(0m);
        Assert.True(pnl.IsBreakEven);
        Assert.False(pnl.IsProfit);
        Assert.False(pnl.IsLoss);
    }

    [Fact]
    public void Calculate_ExitMinusEntry_Works()
    {
        // Bought 10 shares at 0.50 = $5 entry, sold at 0.60 = $6 exit
        var pnl = PnL.Calculate(exitNotional: 6m, entryNotional: 5m);
        Assert.Equal(1m, pnl.Value);
        Assert.True(pnl.IsProfit);
    }

    [Fact]
    public void Calculate_LossScenario()
    {
        // Bought at 0.50 = $5, settled as loss
        var pnl = PnL.Calculate(exitNotional: 4m, entryNotional: 5m);
        Assert.Equal(-1m, pnl.Value);
        Assert.True(pnl.IsLoss);
    }

    [Fact]
    public void Operators_Addition_Works()
    {
        var a = PnL.Create(100m);
        var b = PnL.Create(-30m);
        var result = a + b;
        Assert.Equal(70m, result.Value);
    }

    [Fact]
    public void Operators_Multiplication_Works()
    {
        var pnl = PnL.Create(100m);
        var result = pnl * 0.5m;
        Assert.Equal(50m, result.Value);
    }

    [Fact]
    public void Zero_IsBreakEven()
    {
        var zero = PnL.Zero;
        Assert.Equal(0m, zero.Value);
        Assert.True(zero.IsBreakEven);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var profit = PnL.Create(10m);
        var loss = PnL.Create(-5m);
        Assert.StartsWith("+", profit.ToString());
        Assert.StartsWith("-", loss.ToString());
        Assert.Contains("10", profit.ToString());
        Assert.Contains("5", loss.ToString());
    }

    [Fact]
    public void Comparison_SortsCorrectly()
    {
        var loss = PnL.Create(-50m);
        var zero = PnL.Zero;
        var profit = PnL.Create(100m);

        Assert.True(loss < zero);
        Assert.True(zero < profit);
        Assert.True(profit > loss);
    }
}
