using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Application.Services;
using Polymarket.Bot.Domain.Entities;
using Polymarket.Bot.Domain.Enums;
using Polymarket.Bot.Infrastructure.External.PolymarketClient;
using Polymarket.Bot.Infrastructure.Persistence.DbContext;

namespace Polymarket.Bot.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // PostgreSQL
        services.AddDbContext<PolymarketDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.CommandTimeout(30);
            }));

        // Redis cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString.Replace("Host=localhost;Port=5432", "localhost:6379")
                .Replace("Database=polymarket_bot", "");
            options.InstanceName = "PolymarketBot:";
        });

        // Infrastructure implementations
        services.AddScoped<IAssetPriceFeed, StubAssetPriceFeed>();
        services.AddScoped<StubTrendAnalyzer>();
        services.AddScoped<IStopLossExitExecutor, PaperStopLossExitExecutor>();
        services.AddScoped<IStopLossMonitor, StopLossMonitor>();
        services.AddScoped<RiskManager>();
        services.AddScoped<IPaperOrderExecutor, PaperOrderExecutor>();
        services.AddScoped<IOrderFillSimulator, OrderFillSimulator>();
        services.AddScoped<ITradeExecutionPipeline, TradeExecutionPipeline>();
        services.AddScoped<IStrategyEvaluator, HighProb99CStrategyEvaluator>();
        services.AddScoped<IPolymarketClient, Polymarket.Bot.Infrastructure.External.PolymarketClient.StubPolymarketClient>();

        // Repositories (stub implementations for MVP)
        services.AddScoped<IMarketRepository, InMemoryMarketRepository>();
        services.AddScoped<IPositionRepository, InMemoryPositionRepository>();
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<ITradeRepository, InMemoryTradeRepository>();
        services.AddScoped<IDecisionTraceRepository, InMemoryDecisionTraceRepository>();
        services.AddScoped<IExecutionCycleRepository, InMemoryExecutionCycleRepository>();
        services.AddScoped<IAssetTrendRepository, InMemoryAssetTrendRepository>();

        return services;
    }
}

// In-memory repository stubs for MVP (replace with EF Core-backed in production)
public class InMemoryMarketRepository : IMarketRepository
{
    private readonly List<Market> _markets = new();
    public Task<Market?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_markets.FirstOrDefault(m => m.Id == id));
    public Task<Market?> GetByMarketIdAsync(string marketId, CancellationToken ct = default)
        => Task.FromResult(_markets.FirstOrDefault(m => m.MarketId == marketId));
    public Task<IReadOnlyList<Market>> GetActiveAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Market>>(_markets.ToList());
    public Task<Market> AddAsync(Market market, CancellationToken ct = default)
        { _markets.Add(market); return Task.FromResult(market); }
    public Task UpdateAsync(Market market, CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryPositionRepository : IPositionRepository
{
    private readonly List<Position> _positions = new();
    public Task<Position?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_positions.FirstOrDefault(p => p.Id == id));
    public Task<IReadOnlyList<Position>> GetOpenAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Position>>(_positions.Where(p => p.IsOpen).ToList());
    public Task<IReadOnlyList<Position>> GetClosedAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Position>>(_positions.Where(p => p.IsClosed).ToList());
    public Task<IReadOnlyList<Position>> GetByMarketIdAsync(string marketTicker, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Position>>(_positions.Where(p => p.MarketTicker == marketTicker).ToList());
    public Task<Position> AddAsync(Position position, CancellationToken ct = default)
        { _positions.Add(position); return Task.FromResult(position); }
    public Task UpdateAsync(Position position, CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = new();
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
    public Task<Order?> GetByExternalIdAsync(string orderId, CancellationToken ct = default)
        => Task.FromResult(_orders.FirstOrDefault(o => o.OrderId == orderId));
    public Task<IReadOnlyList<Order>> GetByDecisionIdAsync(Guid decisionId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Order>>(_orders.Where(o => o.TradingDecisionId == decisionId).ToList());
    public Task<Order> AddAsync(Order order, CancellationToken ct = default)
        { _orders.Add(order); return Task.FromResult(order); }
    public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryTradeRepository : ITradeRepository
{
    private readonly List<Trade> _trades = new();
    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_trades.FirstOrDefault(t => t.Id == id));
    public Task<IReadOnlyList<Trade>> GetOpenAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Trade>>(_trades.Where(t => t.IsOpen).ToList());
    public Task<IReadOnlyList<Trade>> GetByPositionIdAsync(Guid positionId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Trade>>(_trades.Where(t => t.PositionId == positionId).ToList());
    public Task<Trade> AddAsync(Trade trade, CancellationToken ct = default)
        { _trades.Add(trade); return Task.FromResult(trade); }
    public Task UpdateAsync(Trade trade, CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryDecisionTraceRepository : IDecisionTraceRepository
{
    private readonly List<DecisionTrace> _traces = new();
    public Task<DecisionTrace?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_traces.FirstOrDefault(t => t.Id == id));
    public Task<IReadOnlyList<DecisionTrace>> GetByExecutionCycleIdAsync(Guid cycleId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DecisionTrace>>(_traces.Where(t => t.ExecutionCycleId == cycleId).ToList());
    public Task<IReadOnlyList<DecisionTrace>> GetByMarketIdAsync(string marketId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DecisionTrace>>(_traces.Where(t => true).ToList()); // stub
    public Task<IReadOnlyList<DecisionTrace>> GetByTypeAsync(string traceType, int limit = 100, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DecisionTrace>>(_traces.Where(t => t.TraceType == traceType).Take(limit).ToList());
    public Task<DecisionTrace> AddAsync(DecisionTrace trace, CancellationToken ct = default)
        { _traces.Add(trace); return Task.FromResult(trace); }
    public Task UpdateAsync(DecisionTrace trace, CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryExecutionCycleRepository : IExecutionCycleRepository
{
    private readonly List<ExecutionCycle> _cycles = new();
    public Task<ExecutionCycle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_cycles.FirstOrDefault(c => c.Id == id));
    public Task<IReadOnlyList<ExecutionCycle>> GetByBotRunIdAsync(Guid botRunId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ExecutionCycle>>(_cycles.Where(c => c.BotRunId == botRunId).ToList());
    public Task<ExecutionCycle> AddAsync(ExecutionCycle cycle, CancellationToken ct = default)
        { _cycles.Add(cycle); return Task.FromResult(cycle); }
    public Task UpdateAsync(ExecutionCycle cycle, CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryAssetTrendRepository : IAssetTrendRepository
{
    private readonly List<AssetTrendSnapshot> _snapshots = new();
    public Task<AssetTrendSnapshot?> GetLatestAsync(AssetSymbol symbol, TrendTimeframe timeframe, CancellationToken ct = default)
        => Task.FromResult(_snapshots.LastOrDefault(s => s.AssetId == Guid.Empty));
    public Task<IReadOnlyList<AssetTrendSnapshot>> GetHistoryAsync(AssetSymbol symbol, TrendTimeframe timeframe, int limit = 100, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AssetTrendSnapshot>>(_snapshots.TakeLast(limit).ToList());
    public Task<AssetTrendSnapshot> AddAsync(AssetTrendSnapshot snapshot, CancellationToken ct = default)
        { _snapshots.Add(snapshot); return Task.FromResult(snapshot); }
}
