using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Workers.Weather.Application.Services;

namespace PolymarketBot.Workers.Weather.Application.UseCases;

public class ScanMarketsUseCase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IWeatherForecastService _forecastService;
    private readonly WeatherSignalGenerator _signalGenerator;
    private readonly ILogger<ScanMarketsUseCase> _logger;

    public ScanMarketsUseCase(
        IMarketDataService marketDataService,
        IWeatherForecastService forecastService,
        WeatherSignalGenerator signalGenerator,
        ILogger<ScanMarketsUseCase> logger)
    {
        _marketDataService = marketDataService;
        _forecastService = forecastService;
        _signalGenerator = signalGenerator;
        _logger = logger;
    }

    public async Task<IEnumerable<TradeSignal>> ExecuteAsync()
    {
        var markets = await _marketDataService.GetOpenMarketsAsync();
        var signals = new List<TradeSignal>();

        foreach (var market in markets)
        {
            try
            {
                var forecast = await _forecastService.GetForecastAsync(market.City, DateOnly.FromDateTime(market.EndDate));
                var signal = _signalGenerator.Generate(market, forecast);
                if (signal != null)
                    signals.Add(signal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process market {MarketId}", market.Id);
            }
        }

        return signals;
    }
}