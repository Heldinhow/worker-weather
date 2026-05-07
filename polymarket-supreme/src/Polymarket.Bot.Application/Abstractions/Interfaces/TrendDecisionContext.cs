using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Context for trend/momentum analysis.
/// </summary>
public record TrendDecisionContext(
    TrendDirection Direction,
    MomentumState Momentum,
    decimal Confidence,
    decimal Strength,
    bool ShouldAllowBullishTrade,
    bool ShouldAllowBearishTrade,
    string? Reason = null);
