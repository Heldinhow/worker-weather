using Microsoft.AspNetCore.Mvc;
using PolymarketBot.Workers.Weather.Application.Services;

namespace PolymarketBot.Api.Endpoints;

public static class MarketEndpoints
{
    public static void MapMarketEndpoints(this WebApplication app)
    {
        app.MapGet("/markets", async (IMarketDataService marketDataService) =>
        {
            var markets = await marketDataService.GetOpenMarketsAsync();
            return Results.Ok(markets);
        }).WithTags("Markets");

        app.MapGet("/markets/{id}", async (string id, IMarketDataService marketDataService) =>
        {
            var market = await marketDataService.GetMarketByIdAsync(id);
            return market == null ? Results.NotFound() : Results.Ok(market);
        }).WithTags("Markets");
    }
}