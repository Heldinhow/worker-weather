using PolymarketBot.Framework.Domain.Entities;

namespace PolymarketBot.Framework.Contracts;

public interface IWorker
{
    string Type { get; }
    WorkerStatus Status { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    Task<IEnumerable<TradeSignal>> ScanMarketsAsync();
}

public enum WorkerStatus { Registered, Running, Stopped, Failed }