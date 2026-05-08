using System.Text.Json;
using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Application.Services;
using StackExchange.Redis;

namespace PolymarketBot.Framework.Infrastructure.Persistence;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiration);
        _logger.LogDebug("Cache set: {Key}, expiration: {Expiration}", key, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
        _logger.LogDebug("Cache removed: {Key}", key);
    }
}