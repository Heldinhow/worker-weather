using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Represents a Polymarket market with one or more outcomes.
/// </summary>
public class Market : Entity
{
    public string MarketId { get; private set; } = string.Empty;  // External Polymarket ID
    public string Slug { get; private set; } = string.Empty;
    public string Question { get; private set; } = string.Empty;
    public MarketStatus Status { get; private set; } = MarketStatus.Open;
    public DateTime? WindowStart { get; private set; }
    public DateTime? WindowEnd { get; private set; }
    public decimal Volume { get; private set; }
    public decimal Liquidity { get; private set; }
    public string? EventSlug { get; private set; }
    public string Platform { get; private set; } = "polymarket";
    public string? MetadataJson { get; private set; }

    private readonly List<MarketOutcome> _outcomes = new();
    public IReadOnlyList<MarketOutcome> Outcomes => _outcomes.AsReadOnly();

    // For EF Core
    private Market() { }

    public static Market Create(
        string marketId,
        string slug,
        string question,
        DateTime? windowStart = null,
        DateTime? windowEnd = null,
        decimal volume = 0,
        decimal liquidity = 0)
    {
        if (string.IsNullOrWhiteSpace(marketId))
            throw new ArgumentException("MarketId cannot be empty", nameof(marketId));

        return new Market
        {
            MarketId = marketId,
            Slug = slug ?? string.Empty,
            Question = question ?? string.Empty,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            Volume = volume,
            Liquidity = liquidity
        };
    }

    public void AddOutcome(MarketOutcome outcome)
    {
        if (outcome is null) throw new ArgumentNullException(nameof(outcome));
        _outcomes.Add(outcome);
    }

    public MarketOutcome? GetOutcome(string outcomeId)
        => _outcomes.FirstOrDefault(o => o.OutcomeId == outcomeId);

    public MarketOutcome? GetOutcomeBySide(OrderSide side)
        => _outcomes.FirstOrDefault(o => o.Side == side);

    public void UpdateStatus(MarketStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        Status = MarketStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive => Status == MarketStatus.Open &&
        WindowEnd > DateTime.UtcNow;

    public double? GetSecondsUntilEnd()
    {
        if (!WindowEnd.HasValue) return null;
        return (WindowEnd.Value - DateTime.UtcNow).TotalSeconds;
    }
}
