using Polymarket.Bot.Domain.ValueObjects;

namespace Polymarket.Bot.Domain.Tests.ValueObjects;

public class ProbabilityTests
{
    [Fact]
    public void Create_WithValidValue_ReturnsProbability()
    {
        var prob = Probability.Create(0.75m);
        Assert.Equal(0.75m, prob.Value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(1.01)]
    [InlineData(2)]
    public void Create_WithInvalidValue_Throws(decimal invalidValue)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Probability.Create(invalidValue));
        Assert.Contains("Probability must be between 0 and 1", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(0.99)]
    public void Create_WithValidBoundaryValues_Succeeds(decimal validValue)
    {
        var prob = Probability.Create(validValue);
        Assert.Equal(validValue, prob.Value);
    }

    [Fact]
    public void Create_RoundsToSixDecimals()
    {
        var prob = Probability.Create(0.123456789m);
        Assert.Equal(0.123457m, prob.Value);
    }

    [Fact]
    public void Operators_Addition_Works()
    {
        var a = Probability.Create(0.3m);
        var b = Probability.Create(0.4m);
        var result = a + b;
        Assert.Equal(0.7m, result.Value);
    }

    [Fact]
    public void Operators_Multiplication_Scalar_Works()
    {
        var prob = Probability.Create(0.5m);
        var result = prob * 2m;
        Assert.Equal(1m, result.Value);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_Works()
    {
        var prob = Probability.Create(0.8m);
        decimal value = prob;
        Assert.Equal(0.8m, value);
    }

    [Fact]
    public void ToString_ReturnsFormattedPercentage()
    {
        var prob = Probability.Create(0.75m);
        Assert.Contains("75", prob.ToString());
    }

    [Fact]
    public void Static_FiftyPercent_IsCorrect()
    {
        var prob = Probability.FiftyPercent;
        Assert.Equal(0.5m, prob.Value);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Probability.Create(0.5m);
        var b = Probability.Create(0.5m);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = Probability.Create(0.5m);
        var b = Probability.Create(0.51m);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_SortsCorrectly()
    {
        var low = Probability.Create(0.3m);
        var mid = Probability.Create(0.5m);
        var high = Probability.Create(0.8m);

        Assert.True(low < mid);
        Assert.True(mid < high);
        Assert.True(high > low);
        Assert.True(mid >= low);
        Assert.True(low <= mid);
    }
}
