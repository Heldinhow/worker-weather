using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Snapshot of inputs at the time of decision.
/// </summary>
public class DecisionInputSnapshot : Entity
{
    public Guid DecisionTraceId { get; private set; }
    public string MarketId { get; private set; } = string.Empty;
    public string MarketSlug { get; private set; } = string.Empty;
    public string OutcomeId { get; private set; } = string.Empty;
    public decimal? BtcPrice { get; private set; }
    public decimal? EthPrice { get; private set; }
    public decimal? MarketPrice { get; private set; }
    public decimal? BestBid { get; private set; }
    public decimal? BestAsk { get; private set; }
    public decimal? Spread { get; private set; }
    public decimal? Liquidity { get; private set; }
    public decimal? Volume24h { get; private set; }
    public double? TimeToResolutionSeconds { get; private set; }
    public TrendDirection? TrendDirection { get; private set; }
    public MomentumState? MomentumState { get; private set; }
    public decimal? TrendConfidence { get; private set; }
    public decimal? TrendStrength { get; private set; }
    public string? MetadataJson { get; private set; }

    private DecisionInputSnapshot() { }

    public static DecisionInputSnapshot Create(
        Guid decisionTraceId,
        string marketId,
        string marketSlug,
        string outcomeId,
        decimal? marketPrice = null,
        decimal? bestBid = null,
        decimal? bestAsk = null,
        decimal? liquidity = null,
        decimal? volume24h = null,
        double? timeToResolutionSeconds = null,
        decimal? btcPrice = null,
        decimal? ethPrice = null,
        TrendDirection? trendDirection = null,
        MomentumState? momentumState = null,
        decimal? trendConfidence = null,
        decimal? trendStrength = null)
    {
        return new DecisionInputSnapshot
        {
            DecisionTraceId = decisionTraceId,
            MarketId = marketId,
            MarketSlug = marketSlug,
            OutcomeId = outcomeId,
            MarketPrice = marketPrice,
            BestBid = bestBid,
            BestAsk = bestAsk,
            Spread = bestAsk.HasValue && bestBid.HasValue ? bestAsk - bestBid : null,
            Liquidity = liquidity,
            Volume24h = volume24h,
            TimeToResolutionSeconds = timeToResolutionSeconds,
            BtcPrice = btcPrice,
            EthPrice = ethPrice,
            TrendDirection = trendDirection,
            MomentumState = momentumState,
            TrendConfidence = trendConfidence,
            TrendStrength = trendStrength
        };
    }
}
