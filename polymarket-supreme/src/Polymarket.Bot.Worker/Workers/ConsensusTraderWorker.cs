using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Worker.Workers;

public class ConsensusTraderWorker : BackgroundService
{
    private readonly IWeatherCache _weatherCache;
    private readonly ITradeExecutionPipeline _tradePipeline;
    private readonly ILogger<ConsensusTraderWorker> _logger;

    public ConsensusTraderWorker(
        IWeatherCache weatherCache,
        ITradeExecutionPipeline tradePipeline,
        ILogger<ConsensusTraderWorker> logger)
    {
        _weatherCache = weatherCache ?? throw new ArgumentNullException(nameof(weatherCache));
        _tradePipeline = tradePipeline ?? throw new ArgumentNullException(nameof(tradePipeline));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConsensusTraderWorker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsensusCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConsensusTraderWorker cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("ConsensusTraderWorker stopping...");
    }

    private async Task ConsensusCycleAsync(CancellationToken ct)
    {
        var cities = await _weatherCache.GetCachedCitiesAsync(ct);
        var tradesPlaced = 0;

        foreach (var city in cities)
        {
            try
            {
                var market = await _weatherCache.GetMarketAsync(city, ct);
                if (market == null)
                {
                    _logger.LogDebug("No market cached for {City}", city);
                    continue;
                }

                var weather = await _weatherCache.GetWeatherAsync(city, ct);
                if (!HasConsensus(weather))
                {
                    _logger.LogDebug("No consensus for {City}: {ServiceCount} services", city, weather.Count);
                    continue;
                }

                _logger.LogInformation("Consensus found for {City}: all services predict rain (prob >= 70%)", city);

                var request = new TradeExecutionRequest(
                    ExecutionCycleId: Guid.NewGuid(),
                    DecisionTraceId: Guid.NewGuid(),
                    MarketId: market.MarketId,
                    MarketSlug: market.MarketId,
                    OutcomeId: "yes",
                    Side: OrderSide.Buy,
                    EntryPrice: market.YesPrice,
                    BestBid: market.BestBid,
                    BestAsk: market.BestAsk,
                    Liquidity: market.Liquidity,
                    Size: 10m,
                    TimeToResolutionSeconds: market.WindowEnd.HasValue
                        ? (market.WindowEnd.Value - DateTime.UtcNow).TotalSeconds
                        : null);

                var result = await _tradePipeline.ExecuteAsync(request, ct);
                if (result.Executed)
                {
                    _logger.LogInformation(
                        "Trade executed for {City}: OrderId={OrderId}, Price={Price}, Shares={Shares}",
                        city,
                        result.OrderId,
                        result.EntryPrice,
                        result.Shares);
                    tradesPlaced++;
                }
                else
                {
                    _logger.LogWarning("Trade rejected for {City}: {Reason}", city, result.RejectReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing {City}", city);
            }
        }

        _logger.LogInformation(
            "Consensus cycle complete: {CityCount} cities checked, {TradesPlaced} trades placed",
            cities.Count,
            tradesPlaced);
    }

    private static bool HasConsensus(IReadOnlyList<WeatherDataDto> readings) =>
        readings.Count >= 2 && readings.All(r => r.RainProbability >= 70.0);
}
