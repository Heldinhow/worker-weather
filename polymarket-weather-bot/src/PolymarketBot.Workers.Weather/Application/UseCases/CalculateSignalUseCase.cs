using FluentResults;
using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Workers.Weather.Application.Services;
using PolymarketBot.Workers.Weather.Domain.Entities;

namespace PolymarketBot.Workers.Weather.Application.UseCases;

public class CalculateSignalUseCase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IWeatherForecastService _forecastService;
    private readonly WeatherSignalGenerator _signalGenerator;
    private readonly ILogger<CalculateSignalUseCase> _logger;

    public CalculateSignalUseCase(
        IMarketDataService marketDataService,
        IWeatherForecastService forecastService,
        WeatherSignalGenerator signalGenerator,
        ILogger<CalculateSignalUseCase> logger)
    {
        _marketDataService = marketDataService;
        _forecastService = forecastService;
        _signalGenerator = signalGenerator;
        _logger = logger;
    }

    public async Task<Result<TradeSignal>> ExecuteAsync(string marketId, DateOnly forecastDate)
    {
        var market = await _marketDataService.GetMarketByIdAsync(marketId);
        if (market == null)
            return Result.Fail($"Market not found: {marketId}");

        var forecast = await _forecastService.GetForecastAsync(market.City, forecastDate);
        var signal = _signalGenerator.Generate(market, forecast);

        if (signal == null)
            return Result.Fail("No signal generated - edge below threshold");

        return Result.Ok(signal);
    }
}