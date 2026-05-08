using Microsoft.Extensions.Logging;
using PolymarketBot.Workers.Weather.Application.Services;
using PolymarketBot.Workers.Weather.Domain.Entities;

namespace PolymarketBot.Workers.Weather.Infrastructure.External;

public class PolymarketMarketDataService : IMarketDataService
{
    private readonly ILogger<PolymarketMarketDataService> _logger;

    public PolymarketMarketDataService(ILogger<PolymarketMarketDataService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<WeatherMarket>> GetOpenMarketsAsync()
    {
        // Return empty list - real implementation would call Polymarket API
        return Task.FromResult(Enumerable.Empty<WeatherMarket>());
    }

    public Task<WeatherMarket?> GetMarketByIdAsync(string marketId)
    {
        // Return null - real implementation would call Polymarket API
        return Task.FromResult<WeatherMarket?>(null);
    }
}
