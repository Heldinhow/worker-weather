using PolymarketBot.Framework.Contracts;

namespace PolymarketBot.Api.BackgroundServices;

public class ScanLoopService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScanLoopService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(300);

    public ScanLoopService(IServiceProvider serviceProvider, ILogger<ScanLoopService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scan loop service starting");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var workers = scope.ServiceProvider.GetServices<IWorker>();
                foreach (var worker in workers)
                {
                    if (worker.Status == WorkerStatus.Running)
                    {
                        await worker.ScanMarketsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scan loop");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
