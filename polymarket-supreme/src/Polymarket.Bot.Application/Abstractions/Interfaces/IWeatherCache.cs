namespace Polymarket.Bot.Application.Abstractions.Interfaces;

public interface IWeatherCache
{
    Task SetMarketAsync(string city, MarketCacheEntry entry, CancellationToken ct = default);
    Task<MarketCacheEntry?> GetMarketAsync(string city, CancellationToken ct = default);
    Task SetWeatherAsync(string city, WeatherDataDto data, CancellationToken ct = default);
    Task<IReadOnlyList<WeatherDataDto>> GetWeatherAsync(string city, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetCachedCitiesAsync(CancellationToken ct = default);
}

public record MarketCacheEntry(
    string MarketId,
    string City,
    string Question,
    decimal YesPrice,
    decimal BestBid,
    decimal BestAsk,
    decimal Liquidity,
    DateTime? WindowEnd);
