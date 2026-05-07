namespace Polymarket.Bot.Domain.Entities;

/// <summary>
/// Snapshot of asset price at a point in time.
/// </summary>
public class AssetPriceSnapshot : Entity
{
    public Guid AssetId { get; private set; }
    public decimal Price { get; private set; }
    public decimal? High24h { get; private set; }
    public decimal? Low24h { get; private set; }
    public decimal? Volume24h { get; private set; }
    public string Source { get; private set; } = "unknown";
    public DateTime CapturedAt { get; private set; } = DateTime.UtcNow;

    private Asset? _asset;
    public Asset? Asset => _asset;

    private AssetPriceSnapshot() { }

    public static AssetPriceSnapshot Create(
        Guid assetId,
        decimal price,
        string source = "stub",
        decimal? high24h = null,
        decimal? low24h = null,
        decimal? volume24h = null)
    {
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive");

        return new AssetPriceSnapshot
        {
            AssetId = assetId,
            Price = Math.Round(price, 2),
            High24h = high24h.HasValue ? Math.Round(high24h.Value, 2) : null,
            Low24h = low24h.HasValue ? Math.Round(low24h.Value, 2) : null,
            Volume24h = volume24h.HasValue ? Math.Round(volume24h.Value, 2) : null,
            Source = source
        };
    }
}
