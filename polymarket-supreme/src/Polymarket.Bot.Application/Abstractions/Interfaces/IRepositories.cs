using Polymarket.Bot.Domain.Entities;

namespace Polymarket.Bot.Application.Abstractions.Interfaces;

/// <summary>
/// Repositories for persistence.
/// </summary>
public interface IMarketRepository
{
    Task<Market?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Market?> GetByMarketIdAsync(string marketId, CancellationToken ct = default);
    Task<IReadOnlyList<Market>> GetActiveAsync(CancellationToken ct = default);
    Task<Market> AddAsync(Market market, CancellationToken ct = default);
    Task UpdateAsync(Market market, CancellationToken ct = default);
}

public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Position>> GetOpenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Position>> GetClosedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Position>> GetByMarketIdAsync(string marketTicker, CancellationToken ct = default);
    Task<Position> AddAsync(Position position, CancellationToken ct = default);
    Task UpdateAsync(Position position, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByExternalIdAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByDecisionIdAsync(Guid decisionId, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}

public interface ITradeRepository
{
    Task<Trade?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Trade>> GetOpenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Trade>> GetByPositionIdAsync(Guid positionId, CancellationToken ct = default);
    Task<Trade> AddAsync(Trade trade, CancellationToken ct = default);
    Task UpdateAsync(Trade trade, CancellationToken ct = default);
}

public interface IDecisionTraceRepository
{
    Task<DecisionTrace?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DecisionTrace>> GetByExecutionCycleIdAsync(Guid cycleId, CancellationToken ct = default);
    Task<IReadOnlyList<DecisionTrace>> GetByMarketIdAsync(string marketId, CancellationToken ct = default);
    Task<IReadOnlyList<DecisionTrace>> GetByTypeAsync(string traceType, int limit = 100, CancellationToken ct = default);
    Task<DecisionTrace> AddAsync(DecisionTrace trace, CancellationToken ct = default);
    Task UpdateAsync(DecisionTrace trace, CancellationToken ct = default);
}

public interface IStrategyRepository
{
    Task<Strategy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Strategy?> GetByTypeAsync(Domain.Enums.StrategyType type, CancellationToken ct = default);
    Task<IReadOnlyList<Strategy>> GetAllAsync(CancellationToken ct = default);
    Task<Strategy> AddAsync(Strategy strategy, CancellationToken ct = default);
    Task UpdateAsync(Strategy strategy, CancellationToken ct = default);
}

public interface IExecutionCycleRepository
{
    Task<ExecutionCycle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExecutionCycle>> GetByBotRunIdAsync(Guid botRunId, CancellationToken ct = default);
    Task<ExecutionCycle> AddAsync(ExecutionCycle cycle, CancellationToken ct = default);
    Task UpdateAsync(ExecutionCycle cycle, CancellationToken ct = default);
}

public interface IAssetTrendRepository
{
    Task<AssetTrendSnapshot?> GetLatestAsync(Domain.Enums.AssetSymbol symbol, Domain.Enums.TrendTimeframe timeframe, CancellationToken ct = default);
    Task<IReadOnlyList<AssetTrendSnapshot>> GetHistoryAsync(Domain.Enums.AssetSymbol symbol, Domain.Enums.TrendTimeframe timeframe, int limit = 100, CancellationToken ct = default);
    Task<AssetTrendSnapshot> AddAsync(AssetTrendSnapshot snapshot, CancellationToken ct = default);
}
