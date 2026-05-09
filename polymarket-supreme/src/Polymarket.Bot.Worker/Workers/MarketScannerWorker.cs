using Polymarket.Bot.Application.Abstractions.Interfaces;
using System.Text.RegularExpressions;

namespace Polymarket.Bot.Worker.Workers;

public class MarketScannerWorker : BackgroundService
{
    private readonly IPolymarketClient _polymarketClient;
    private readonly IWeatherCache _weatherCache;
    private readonly IEnumerable<IWeatherService> _weatherServices;
    private readonly ILogger<MarketScannerWorker> _logger;

    public MarketScannerWorker(
        IPolymarketClient polymarketClient,
        IWeatherCache weatherCache,
        IEnumerable<IWeatherService> weatherServices,
        ILogger<MarketScannerWorker> logger)
    {
        _polymarketClient = polymarketClient ?? throw new ArgumentNullException(nameof(polymarketClient));
        _weatherCache = weatherCache ?? throw new ArgumentNullException(nameof(weatherCache));
        _weatherServices = weatherServices ?? throw new ArgumentNullException(nameof(weatherServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketScannerWorker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarketScannerWorker cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("MarketScannerWorker stopping...");
    }

    private async Task ScanCycleAsync(CancellationToken ct)
    {
        var markets = await _polymarketClient.GetActiveMarketsAsync(ct);
        var filtered = markets
            .Where(m => m.YesPrice >= 0.87m && m.YesPrice <= 0.97m)
            .ToList();

        var cities = new HashSet<string>();

        foreach (var market in filtered)
        {
            var city = ExtractCity(market.Question);
            if (city == null) continue;

            cities.Add(city);

            var entry = new MarketCacheEntry(
                market.MarketId,
                city,
                market.Question,
                market.YesPrice,
                market.BestBid,
                market.BestAsk,
                market.Liquidity,
                market.WindowEnd);

            await _weatherCache.SetMarketAsync(city, entry, ct);
            _logger.LogDebug("Cached market for {City}: {Question}", city, market.Question);
        }

        foreach (var city in cities)
        {
            try
            {
                var tasks = _weatherServices.Select(svc => svc.GetWeatherAsync(city, ct)).ToList();
                var results = await Task.WhenAll(tasks);

                foreach (var result in results.Where(r => r != null))
                {
                    await _weatherCache.SetWeatherAsync(city, result!, ct);
                }

                _logger.LogDebug("Fetched weather for {City} from {ServiceCount} services", city, results.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching weather for {City}", city);
            }
        }

        _logger.LogInformation(
            "Market scan complete: {MarketCount} markets filtered, {CityCount} cities found",
            filtered.Count,
            cities.Count);
    }

    private string? ExtractCity(string question)
    {
        var match = Regex.Match(question, @"in\s+([A-Z][a-zA-Z\s]+)\s+on");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
