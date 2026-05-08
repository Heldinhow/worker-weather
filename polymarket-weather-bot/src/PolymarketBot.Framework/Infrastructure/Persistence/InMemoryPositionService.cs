using FluentResults;
using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Application.Services;
using PolymarketBot.Framework.Domain.Entities;

namespace PolymarketBot.Framework.Infrastructure.Persistence;

public class InMemoryPositionService : IPositionService
{
    private readonly List<Position> _positions = new();
    private readonly ILogger<InMemoryPositionService> _logger;

    public InMemoryPositionService(ILogger<InMemoryPositionService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<Position>> GetOpenPositionsAsync()
    {
        return Task.FromResult(_positions.Where(p => p.Status == PositionStatus.Open).AsEnumerable());
    }

    public Task<Result<Position>> OpenPositionAsync(TradeSignal signal)
    {
        var position = new Position
        {
            Id = Guid.NewGuid(),
            MarketId = signal.MarketId,
            Direction = signal.Direction,
            EntryPrice = signal.EntryPrice,
            CurrentPrice = signal.EntryPrice,
            Size = signal.Size,
            Status = PositionStatus.Open,
            OpenedAt = DateTime.UtcNow
        };
        _positions.Add(position);
        _logger.LogInformation("Position opened: {PositionId} for market {MarketId}", position.Id, position.MarketId);
        return Task.FromResult(Result.Ok(position));
    }

    public Task<Result<Position>> ClosePositionAsync(Guid positionId)
    {
        var position = _positions.FirstOrDefault(p => p.Id == positionId);
        if (position == null)
            return Task.FromResult(Result.Fail<Position>("Position not found"));

        position.Status = PositionStatus.Closed;
        _logger.LogInformation("Position closed: {PositionId}", positionId);
        return Task.FromResult(Result.Ok(position));
    }

    public Task<Result<Position>> CheckStopsAndTakesAsync(Guid positionId)
    {
        var position = _positions.FirstOrDefault(p => p.Id == positionId);
        if (position == null)
            return Task.FromResult(Result.Fail<Position>("Position not found"));

        // Check stop loss and take profit logic
        var currentPrice = position.CurrentPrice.Value;
        var entryPrice = position.EntryPrice.Value;

        if (position.Direction == Direction.YES)
        {
            // For YES direction, price going down triggers stop loss
            var lossThreshold = entryPrice * (1 - position.StopLossPercent);
            if (currentPrice <= lossThreshold)
            {
                position.Status = PositionStatus.StopLoss;
                _logger.LogInformation("Stop loss triggered for position {PositionId}", positionId);
            }
        }

        return Task.FromResult(Result.Ok(position));
    }
}
