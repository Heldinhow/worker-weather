using FluentResults;
using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Application.Services;
using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Framework.Domain.ValueObjects;

namespace PolymarketBot.Framework.Application.UseCases;

public class ExecuteTradeUseCase
{
    private readonly IPolymarketClient _polymarketClient;
    private readonly IPositionService _positionService;
    private readonly ILogger<ExecuteTradeUseCase> _logger;

    public ExecuteTradeUseCase(IPolymarketClient polymarketClient, IPositionService positionService, ILogger<ExecuteTradeUseCase> logger)
    {
        _polymarketClient = polymarketClient;
        _positionService = positionService;
        _logger = logger;
    }

    public async Task<Result<Position>> ExecuteAsync(TradeSignal signal)
    {
        var orderRequest = new OrderRequest(signal.MarketId, signal.Direction, signal.Size, signal.EntryPrice.Value);
        var orderResult = await _polymarketClient.PlaceOrderAsync(orderRequest);

        if (orderResult.IsFailed)
        {
            _logger.LogError("Failed to place order: {Error}", orderResult.Errors.First().Message);
            return Result.Fail(orderResult.Errors);
        }

        return await _positionService.OpenPositionAsync(signal);
    }
}