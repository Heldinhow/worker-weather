using Polymarket.Bot.Application.Abstractions.Results;

namespace Polymarket.Bot.Application.Services;

/// <summary>
/// Risk manager for evaluating trading decisions.
/// </summary>
public class RiskManager
{
    private readonly RiskManagerSettings _settings;

    public RiskManager(RiskManagerSettings? settings = null)
    {
        _settings = settings ?? new RiskManagerSettings();
    }

    /// <summary>
    /// Evaluate risk rules for a potential trade.
    /// </summary>
    public RiskEvaluationResult Evaluate(
        decimal marketPrice,
        decimal bestBid,
        decimal bestAsk,
        decimal liquidity,
        decimal? volume24h,
        double? timeToResolutionSeconds,
        decimal? dailyPnl,
        int openPositionsCount)
    {
        var evaluations = new List<RiskEvaluationResult>();

        // Rule 1: Spread check
        var spread = bestAsk > 0 && bestBid > 0 ? bestAsk - bestBid : 0m;
        var spreadBps = marketPrice > 0 ? (spread / marketPrice) * 10000m : 0m;
        var spreadOk = spreadBps <= _settings.MaxSpreadBps;
        evaluations.Add(new RiskEvaluationResult(
            "MaxSpread",
            spreadOk,
            spreadOk ? null : $"Spread {spreadBps:N0} bps exceeds max {_settings.MaxSpreadBps:N0} bps",
            _settings.MaxSpreadBps,
            spreadBps));

        // Rule 2: Liquidity check
        var liquidityOk = liquidity >= _settings.MinLiquidity;
        evaluations.Add(new RiskEvaluationResult(
            "MinLiquidity",
            liquidityOk,
            liquidityOk ? null : $"Liquidity ${liquidity:N2} below min ${_settings.MinLiquidity:N2}",
            _settings.MinLiquidity,
            liquidity));

        // Rule 3: Daily loss limit
        if (dailyPnl.HasValue)
        {
            var dailyLossOk = dailyPnl.Value >= -_settings.DailyLossLimit;
            evaluations.Add(new RiskEvaluationResult(
                "DailyLossLimit",
                dailyLossOk,
                dailyLossOk ? null : $"Daily loss ${dailyPnl:N2} exceeds limit ${_settings.DailyLossLimit:N2}",
                -_settings.DailyLossLimit,
                dailyPnl));
        }

        // Rule 4: Max open positions
        var positionsOk = openPositionsCount < _settings.MaxOpenPositions;
        evaluations.Add(new RiskEvaluationResult(
            "MaxOpenPositions",
            positionsOk,
            positionsOk ? null : $"Open positions {openPositionsCount} at max {_settings.MaxOpenPositions}",
            _settings.MaxOpenPositions,
            openPositionsCount));

        // Rule 5: Time to resolution check
        if (timeToResolutionSeconds.HasValue)
        {
            var timeOk = timeToResolutionSeconds.Value >= _settings.MinTimeToResolutionSeconds
                && timeToResolutionSeconds.Value <= _settings.MaxTimeToResolutionSeconds;
            evaluations.Add(new RiskEvaluationResult(
                "TimeToResolution",
                timeOk,
                timeOk ? null : $"Time to resolution {timeToResolutionSeconds:N0}s not in range",
                null,
                (decimal?)timeToResolutionSeconds.Value));
        }

        // Rule 6: Volume check
        if (volume24h.HasValue && _settings.MinVolume24h > 0)
        {
            var volumeOk = volume24h.Value >= _settings.MinVolume24h;
            evaluations.Add(new RiskEvaluationResult(
                "MinVolume24h",
                volumeOk,
                volumeOk ? null : $"Volume ${volume24h:N2} below min ${_settings.MinVolume24h:N2}",
                _settings.MinVolume24h,
                volume24h));
        }

        // Overall result: all rules must pass
        var allPassed = evaluations.All(e => e.IsApproved);
        var failedRule = evaluations.FirstOrDefault(e => !e.IsApproved);

        return new RiskEvaluationResult(
            "Overall",
            allPassed,
            allPassed ? null : failedRule?.Reason ?? "Risk check failed");
    }

    /// <summary>
    /// Get all evaluations from the last evaluation.
    /// </summary>
    public IReadOnlyList<RiskEvaluationResult> GetAllRuleEvaluations(
        decimal marketPrice,
        decimal bestBid,
        decimal bestAsk,
        decimal liquidity,
        decimal? volume24h,
        double? timeToResolutionSeconds,
        decimal? dailyPnl,
        int openPositionsCount)
    {
        var evaluations = new List<RiskEvaluationResult>();

        // Spread
        var spread = bestAsk > 0 && bestBid > 0 ? bestAsk - bestBid : 0m;
        var spreadBps = marketPrice > 0 ? (spread / marketPrice) * 10000m : 0m;
        evaluations.Add(new RiskEvaluationResult(
            "MaxSpread",
            spreadBps <= _settings.MaxSpreadBps,
            null,
            _settings.MaxSpreadBps,
            spreadBps));

        // Liquidity
        evaluations.Add(new RiskEvaluationResult(
            "MinLiquidity",
            liquidity >= _settings.MinLiquidity,
            null,
            _settings.MinLiquidity,
            liquidity));

        // Daily loss
        if (dailyPnl.HasValue)
        {
            evaluations.Add(new RiskEvaluationResult(
                "DailyLossLimit",
                dailyPnl.Value >= -_settings.DailyLossLimit,
                null,
                -_settings.DailyLossLimit,
                dailyPnl));
        }

        // Max positions
        evaluations.Add(new RiskEvaluationResult(
            "MaxOpenPositions",
            openPositionsCount < _settings.MaxOpenPositions,
            null,
            _settings.MaxOpenPositions,
            openPositionsCount));

        // Time to resolution
        if (timeToResolutionSeconds.HasValue)
        {
            var inRange = timeToResolutionSeconds.Value >= _settings.MinTimeToResolutionSeconds
                && timeToResolutionSeconds.Value <= _settings.MaxTimeToResolutionSeconds;
            evaluations.Add(new RiskEvaluationResult(
                "TimeToResolution",
                inRange,
                null,
                null,
                (decimal?)timeToResolutionSeconds.Value));
        }

        // Volume
        if (volume24h.HasValue && _settings.MinVolume24h > 0)
        {
            evaluations.Add(new RiskEvaluationResult(
                "MinVolume24h",
                volume24h.Value >= _settings.MinVolume24h,
                null,
                _settings.MinVolume24h,
                volume24h));
        }

        return evaluations;
    }
}

/// <summary>
/// Risk manager settings.
/// </summary>
public class RiskManagerSettings
{
    public decimal MaxSpreadBps { get; set; } = 100m;
    public decimal MinLiquidity { get; set; } = 25m;
    public decimal DailyLossLimit { get; set; } = 300m;
    public int MaxOpenPositions { get; set; } = 20;
    public double MinTimeToResolutionSeconds { get; set; } = 60;
    public double MaxTimeToResolutionSeconds { get; set; } = 1800;
    public decimal MinVolume24h { get; set; } = 100m;
}
