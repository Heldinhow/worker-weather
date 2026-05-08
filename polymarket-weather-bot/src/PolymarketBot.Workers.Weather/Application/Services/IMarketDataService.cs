using PolymarketBot.Workers.Weather.Domain.Entities;

namespace PolymarketBot.Workers.Weather.Application.Services;

public interface IMarketDataService
{
    Task<IEnumerable<WeatherMarket>> GetOpenMarketsAsync();
    Task<WeatherMarket?> GetMarketByIdAsync(string marketId);
}