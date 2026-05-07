using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Fetches asset (BTC/ETH) prices.
/// </summary>
public interface IAssetPriceFeed
{
    /// <summary>
    /// Get current price for an asset.
    /// </summary>
    Task<AssetPriceDto> GetPriceAsync(AssetSymbol symbol, CancellationToken ct = default);

    /// <summary>
    /// Get price history for an asset.
    /// </summary>
    Task<IReadOnlyList<AssetPriceDto>> GetPriceHistoryAsync(
        AssetSymbol symbol,
        TrendTimeframe timeframe,
        int limit = 60,
        CancellationToken ct = default);
}

/// <summary>
/// Asset price data.
/// </summary>
public record AssetPriceDto(
    AssetSymbol Symbol,
    decimal Price,
    decimal? High24h,
    decimal? Low24h,
    decimal? Volume24h,
    DateTime Timestamp,
    string Source);

/// <summary>
/// Trend analysis result.
/// </summary>
public record AssetTrendDto(
    AssetSymbol Symbol,
    TrendTimeframe Timeframe,
    TrendDirection Direction,
    MomentumState Momentum,
    decimal TrendStrength,
    decimal Confidence,
    decimal Rsi,
    decimal Momentum1m,
    decimal Momentum5m,
    decimal Momentum15m,
    decimal VwapDeviation,
    decimal SmaCrossover,
    bool ShouldAllowBullishTrade,
    bool ShouldAllowBearishTrade,
    string? Reason);
