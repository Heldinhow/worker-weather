using FluentAssertions;
using Polymarket.Bot.Application.Services;

namespace Polymarket.Bot.Application.Tests;

public class RiskManagerTests
{
    private readonly RiskManager _riskManager;

    public RiskManagerTests()
    {
        _riskManager = new RiskManager();
    }

    [Fact]
    public void Evaluate_SpreadOk_ReturnsApproved()
    {
        var result = _riskManager.Evaluate(
            marketPrice: 0.99m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 1000m,
            volume24h: 5000m,
            timeToResolutionSeconds: 300,
            dailyPnl: 50m,
            openPositionsCount: 5);

        result.RuleName.Should().Be("Overall");
        result.IsApproved.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WideSpread_Rejects()
    {
        var result = _riskManager.Evaluate(
            marketPrice: 0.50m,
            bestBid: 0.45m,
            bestAsk: 0.55m, // 10% spread
            liquidity: 1000m,
            volume24h: null,
            timeToResolutionSeconds: 300,
            dailyPnl: null,
            openPositionsCount: 0);

        result.IsApproved.Should().BeFalse();
        result.Reason.Should().Contain("Spread");
    }

    [Fact]
    public void Evaluate_LowLiquidity_Rejects()
    {
        var result = _riskManager.Evaluate(
            marketPrice: 0.99m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 1m, // below min $25
            volume24h: null,
            timeToResolutionSeconds: null,
            dailyPnl: null,
            openPositionsCount: 0);

        result.IsApproved.Should().BeFalse();
        result.Reason.Should().Contain("Liquidity");
    }

    [Fact]
    public void Evaluate_TooShortTime_Rejects()
    {
        var result = _riskManager.Evaluate(
            marketPrice: 0.99m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 1000m,
            volume24h: null,
            timeToResolutionSeconds: 10, // below min 60s
            dailyPnl: null,
            openPositionsCount: 0);

        result.IsApproved.Should().BeFalse();
        result.Reason.Should().Contain("Time to resolution");
    }

    [Fact]
    public void GetAllRuleEvaluations_ReturnsAllRules()
    {
        var evaluations = _riskManager.GetAllRuleEvaluations(
            marketPrice: 0.99m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 1000m,
            volume24h: null,
            timeToResolutionSeconds: 300,
            dailyPnl: null,
            openPositionsCount: 0);

        evaluations.Should().Contain(e => e.RuleName == "MaxSpread");
        evaluations.Should().Contain(e => e.RuleName == "MinLiquidity");
        evaluations.Should().Contain(e => e.RuleName == "MaxOpenPositions");
        evaluations.Should().Contain(e => e.RuleName == "TimeToResolution");
    }

    [Fact]
    public void Evaluate_MultipleRulesAllPass_ReturnsApproved()
    {
        var result = _riskManager.Evaluate(
            marketPrice: 0.99m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 100m,
            volume24h: null,
            timeToResolutionSeconds: 300,
            dailyPnl: 0m,
            openPositionsCount: 5);

        result.IsApproved.Should().BeTrue();
        result.Reason.Should().BeNull();
    }
}
