using FluentResults;
using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Application.Services;

namespace PolymarketBot.Framework.Application.UseCases;

public class MonitorPositionsUseCase
{
    private readonly IPositionService _positionService;
    private readonly ILogger<MonitorPositionsUseCase> _logger;

    public MonitorPositionsUseCase(IPositionService positionService, ILogger<MonitorPositionsUseCase> logger)
    {
        _positionService = positionService;
        _logger = logger;
    }

    public async Task<Result> MonitorAsync()
    {
        var positions = await _positionService.GetOpenPositionsAsync();
        foreach (var position in positions)
        {
            var result = await _positionService.CheckStopsAndTakesAsync(position.Id);
            if (result.IsFailed)
            {
                _logger.LogError("Failed to check stops/takes for position {PositionId}: {Error}", position.Id, result.Errors.First().Message);
            }
        }
        return Result.Ok();
    }
}