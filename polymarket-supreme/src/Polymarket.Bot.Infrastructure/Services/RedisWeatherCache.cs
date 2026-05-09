using Microsoft.Extensions.Caching.Distributed;
using Polymarket.Bot.Application.Abstractions.Interfaces;
using System.Text.Json;

namespace Polymarket.Bot.Infrastructure.Services;

public class RedisWeatherCache : IWeatherCache
{
    private readonly IDistributedCache _cache;

    public RedisWeatherCache(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task SetMarketAsync(string city, MarketCacheEntry entry, CancellationToken ct = default)
    {
        var key = $"market:{city.ToLower()}";
        var json = JsonSerializer.Serialize(entry);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(key, json, options, ct);
        await AddToCityListAsync(city, ct);
    }

    public async Task<MarketCacheEntry?> GetMarketAsync(string city, CancellationToken ct = default)
    {
        var key = $"market:{city.ToLower()}";
        var json = await _cache.GetStringAsync(key, ct);
        return json != null ? JsonSerializer.Deserialize<MarketCacheEntry>(json) : null;
    }

    public async Task SetWeatherAsync(string city, WeatherDataDto data, CancellationToken ct = default)
    {
        var key = $"weather:{city.ToLower()}";
        var existingJson = await _cache.GetStringAsync(key, ct);
        var readings = existingJson != null
            ? JsonSerializer.Deserialize<List<WeatherDataDto>>(existingJson) ?? new()
            : new List<WeatherDataDto>();

        var existing = readings.FirstOrDefault(r => r.ServiceName == data.ServiceName);
        if (existing != null)
            readings.Remove(existing);
        readings.Add(data);

        var json = JsonSerializer.Serialize(readings);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await _cache.SetStringAsync(key, json, options, ct);
        await AddToCityListAsync(city, ct);
    }

    public async Task<IReadOnlyList<WeatherDataDto>> GetWeatherAsync(string city, CancellationToken ct = default)
    {
        var key = $"weather:{city.ToLower()}";
        var json = await _cache.GetStringAsync(key, ct);
        if (json == null) return Array.Empty<WeatherDataDto>();

        var readings = JsonSerializer.Deserialize<List<WeatherDataDto>>(json) ?? new();
        return readings.AsReadOnly();
    }

    public async Task<IReadOnlyList<string>> GetCachedCitiesAsync(CancellationToken ct = default)
    {
        var key = "cities";
        var json = await _cache.GetStringAsync(key, ct);
        if (json == null) return Array.Empty<string>();

        var cities = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        return cities.AsReadOnly();
    }

    private async Task AddToCityListAsync(string city, CancellationToken ct)
    {
        var key = "cities";
        var json = await _cache.GetStringAsync(key, ct);
        var cities = json != null
            ? JsonSerializer.Deserialize<HashSet<string>>(json) ?? new()
            : new HashSet<string>();

        cities.Add(city);
        var updated = JsonSerializer.Serialize(cities.ToList());
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };
        await _cache.SetStringAsync(key, updated, options, ct);
    }
}
