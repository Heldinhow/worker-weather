using FluentResults;
using Microsoft.Extensions.Logging;
using PolymarketBot.Framework.Application.Services;
using PolymarketBot.Framework.Domain.Entities;

namespace PolymarketBot.Framework.Application.UseCases;

public class ResolveTradesUseCase
{
    private readonly IPositionService _positionService;
    private readonly IPolymarketClient _polymarketClient;
    private readonly ILogger<ResolveTradesUseCase> _logger;

    public ResolveTradesUseCase(IPositionService positionService, IPolymarketClient polymarketClient, ILogger<ResolveTradesUseCase> logger)
    {
        _positionService = positionService;
        _polymarketClient = polymarketClient;
        _logger = logger;
    }

    public async Task<Result> ResolveAsync(string marketId)
    {
        var statusResult = await _polymarketClient.GetMarketStatusAsync(marketId);
        if (statusResult.IsFailed)
            return Result.Fail(statusResult.Errors);

        if (statusResult.Value != MarketStatus.Resolved)
            return Result.Ok(); // Market not yet resolved

        // Auto-resolve logic here
        return Result.Ok();
    }
}