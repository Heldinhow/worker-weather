using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Contracts;
using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Workers.Weather.Application.UseCases;

namespace PolymarketBot.Workers.Weather;

public class WeatherWorker : IWorker
{
    private readonly ScanMarketsUseCase _scanMarketsUseCase;
    private readonly ILogger<WeatherWorker> _logger;

    public string Type => "Weather";
    public WorkerStatus Status { get; private set; }

    public WeatherWorker(ScanMarketsUseCase scanMarketsUseCase, ILogger<WeatherWorker> logger)
    {
        _scanMarketsUseCase = scanMarketsUseCase;
        _logger = logger;
        Status = WorkerStatus.Registered;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        Status = WorkerStatus.Running;
        _logger.LogInformation("Weather worker started");
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        Status = WorkerStatus.Stopped;
        _logger.LogInformation("Weather worker stopped");
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<TradeSignal>> ScanMarketsAsync()
    {
        _logger.LogDebug("Scanning markets...");
        return await _scanMarketsUseCase.ExecuteAsync();
    }
}