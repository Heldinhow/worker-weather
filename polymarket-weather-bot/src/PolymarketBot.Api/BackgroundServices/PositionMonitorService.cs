using PolymarketBot.Framework.Application.UseCases;

namespace PolymarketBot.Api.BackgroundServices;

public class PositionMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PositionMonitorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    public PositionMonitorService(IServiceProvider serviceProvider, ILogger<PositionMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Position monitor service starting");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var monitorUseCase = scope.ServiceProvider.GetRequiredService<MonitorPositionsUseCase>();
                await monitorUseCase.MonitorAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in position monitor");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
