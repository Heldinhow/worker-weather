using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Snapshot of calculated trend for an asset.
/// </summary>
public class AssetTrendSnapshot : Entity
{
    public Guid AssetId { get; private set; }
    public TrendTimeframe Timeframe { get; private set; } = TrendTimeframe.OneMinute;
    public TrendDirection Direction { get; private set; } = TrendDirection.Unknown;
    public MomentumState Momentum { get; private set; } = MomentumState.Neutral;
    public decimal TrendStrength { get; private set; }    // 0-1
    public decimal Confidence { get; private set; }        // 0-1
    public decimal Rsi { get; private set; }               // 0-100
    public decimal Momentum1m { get; private set; }        // %
    public decimal Momentum5m { get; private set; }        // %
    public decimal Momentum15m { get; private set; }       // %
    public decimal VwapDeviation { get; private set; }      // %
    public decimal SmaCrossover { get; private set; }      // %
    public bool ShouldAllowBullishTrade { get; private set; }
    public bool ShouldAllowBearishTrade { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;
    public string? MetadataJson { get; private set; }

    private Asset? _asset;
    public Asset? Asset => _asset;

    private AssetTrendSnapshot() { }

    public static AssetTrendSnapshot Create(
        Guid assetId,
        TrendTimeframe timeframe,
        TrendDirection direction,
        MomentumState momentum,
        decimal trendStrength,
        decimal confidence,
        decimal rsi = 50,
        decimal momentum1m = 0,
        decimal momentum5m = 0,
        decimal momentum15m = 0,
        decimal vwapDeviation = 0,
        decimal smaCrossover = 0,
        bool shouldAllowBullishTrade = true,
        bool shouldAllowBearishTrade = true,
        string? reason = null)
    {
        return new AssetTrendSnapshot
        {
            AssetId = assetId,
            Timeframe = timeframe,
            Direction = direction,
            Momentum = momentum,
            TrendStrength = Math.Clamp(trendStrength, 0, 1),
            Confidence = Math.Clamp(confidence, 0, 1),
            Rsi = Math.Clamp(rsi, 0, 100),
            Momentum1m = momentum1m,
            Momentum5m = momentum5m,
            Momentum15m = momentum15m,
            VwapDeviation = vwapDeviation,
            SmaCrossover = smaCrossover,
            ShouldAllowBullishTrade = shouldAllowBullishTrade,
            ShouldAllowBearishTrade = shouldAllowBearishTrade,
            Reason = reason
        };
    }
}
