using PolymarketBot.Framework.Application.Services;
using PolymarketBot.Framework.Application.UseCases;

namespace PolymarketBot.Api.BackgroundServices;

public class MarketResolutionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketResolutionService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public MarketResolutionService(IServiceProvider serviceProvider, ILogger<MarketResolutionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market resolution service starting");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var resolveUseCase = scope.ServiceProvider.GetRequiredService<ResolveTradesUseCase>();
                var positionService = scope.ServiceProvider.GetRequiredService<IPositionService>();

                var positions = await positionService.GetOpenPositionsAsync();
                foreach (var position in positions)
                {
                    await resolveUseCase.ResolveAsync(position.MarketId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in market resolution");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
