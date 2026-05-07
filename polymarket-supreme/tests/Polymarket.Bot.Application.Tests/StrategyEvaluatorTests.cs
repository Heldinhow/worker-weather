using FluentAssertions;
using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Services;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Tests;

public class StrategyEvaluatorTests
{
    private readonly HighProb99CStrategyEvaluator _evaluator;
    private readonly Strategy _strategy;

    public StrategyEvaluatorTests()
    {
        _evaluator = new HighProb99CStrategyEvaluator();
        _strategy = Strategy.CreateHighProb99C();

        var market = Market.Create(
            "test-btc-5m",
            "test-btc-5m-slug",
            "Will BTC be above 105,000 at 5:00 PM UTC?",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5),
            volume: 10000m,
            liquidity: 1000m);

        market.AddOutcome(MarketOutcome.Create(
            Guid.NewGuid(),
            "up-token",
            "UP",
            OrderSide.Buy,
            price: 0.99m,
            bestBid: 0.989m,
            bestAsk: 0.991m,
            liquidity: 500m));
    }

    [Fact]
    public void StrategyType_IsHighProb99C()
    {
        _evaluator.StrategyType.Should().Be(StrategyType.HighProb99C);
    }

    [Fact]
    public void Evaluate_HighPrice_LowSpread_PositiveFactors()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 1000m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.99m, bestBid: 0.989m, bestAsk: 0.991m, liquidity: 500m);

        var context = new StrategyInputContext(
            BtcPrice: 105000m,
            EthPrice: 3500m,
            BtcTrend: new TrendDecisionContext(
                TrendDirection.Up, MomentumState.Bullish, 0.7m, 0.3m,
                true, false, "BTC trending up"),
            EthTrend: null,
            TimeToResolutionSeconds: 300,
            Volume24h: 5000m);

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.Action.Should().BeOneOf(SignalAction.Buy, SignalAction.Skip);
        result.PositiveFactors.Should().NotBeEmpty();
    }

    [Fact]
    public void Evaluate_LowLiquidity_Blocked()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 10m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.99m, bestBid: 0.989m, bestAsk: 0.991m, liquidity: 1m);

        var context = new StrategyInputContext(
            105000m, 3500m, null, null, 300, 100m);

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.IsSkip.Should().BeTrue();
        result.BlockingFactors.Should().NotBeEmpty();
        result.BlockingFactors.Should().Contain(f => f.Contains("Liquidity"));
    }

    [Fact]
    public void Evaluate_PriceBelowThreshold_Skipped()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 1000m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.80m, bestBid: 0.79m, bestAsk: 0.81m, liquidity: 500m);

        var context = new StrategyInputContext(
            105000m, 3500m, null, null, 300, 100m);

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.Action.Should().Be(SignalAction.Skip);
        result.SkipReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Evaluate_WideSpread_Blocked()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 1000m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.99m, bestBid: 0.90m, bestAsk: 0.99m, liquidity: 500m);

        var context = new StrategyInputContext(
            105000m, 3500m, null, null, 300, 100m);

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.IsSkip.Should().BeTrue();
        result.BlockingFactors.Should().Contain(f => f.Contains("Spread"));
    }

    [Fact]
    public void Evaluate_TooShortTime_Blocked()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 1000m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.99m, bestBid: 0.989m, bestAsk: 0.991m, liquidity: 500m);

        var context = new StrategyInputContext(
            105000m, 3500m, null, null, 1.0, 100m); // 1 second

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.IsSkip.Should().BeTrue();
        result.BlockingFactors.Should().Contain(f => f.Contains("Time"));
    }

    [Fact]
    public void Evaluate_ContrarianTrend_Blocked()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 1000m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.99m, bestBid: 0.989m, bestAsk: 0.991m, liquidity: 500m);

        // BTC trending DOWN but we're trying to BUY (bullish)
        var bearishTrend = new TrendDecisionContext(
            TrendDirection.Down, MomentumState.Bearish, 0.8m, 0.5m,
            false, true, "BTC strongly bearish");

        var context = new StrategyInputContext(
            105000m, 3500m, bearishTrend, null, 300, 100m);

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.IsSkip.Should().BeTrue();
        result.BlockingFactors.Should().Contain(f => f.Contains("trend") || f.Contains("Trend"));
    }

    [Fact]
    public void Skip_HasStructuredReason()
    {
        var market = Market.Create("t1", "s1", "q1", liquidity: 1m);
        var outcome = MarketOutcome.Create(
            Guid.NewGuid(), "token", "UP", OrderSide.Buy,
            price: 0.99m, bestBid: 0.989m, bestAsk: 0.991m, liquidity: 1m);

        var context = new StrategyInputContext(105000m, 3500m, null, null, 300, 100m);

        var result = _evaluator.Evaluate(market, outcome, _strategy, context);

        result.SkipReason.Should().NotBeNullOrEmpty();
        result.SkipReason.Should().Contain("BLOCKED");
    }
}
