using FluentResults;
using PolymarketBot.Framework.Domain.Entities;

namespace PolymarketBot.Framework.Application.Services;

public interface IPositionService
{
    Task<Result<Position>> OpenPositionAsync(TradeSignal signal);
    Task<Result<Position>> ClosePositionAsync(Guid positionId);
    Task<Result<Position>> CheckStopsAndTakesAsync(Guid positionId);
    Task<IEnumerable<Position>> GetOpenPositionsAsync();
}