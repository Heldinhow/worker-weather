using Polymarket.Bot.Api.MinimalApiExtensions;
using Polymarket.Bot.Application.Abstractions.Interfaces;

namespace Polymarket.Bot.Api.Endpoints;

public static class MarketsEndpoints
{
    public static IEndpointRouteBuilder MapMarketsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/markets", async (IPolymarketClient client, CancellationToken ct) =>
        {
            var markets = await client.GetActiveMarketsAsync(ct);
            return Results.Ok(markets.Select(m => new
            {
                m.MarketId,
                m.Slug,
                m.Question,
                m.Status,
                m.YesPrice,
                m.NoPrice,
                m.BestBid,
                m.BestAsk,
                m.Liquidity,
                m.Volume,
                m.WindowStart,
                m.WindowEnd
            }));
        }).WithDescription("Get active Polymarket markets");

        app.MapGet("/api/markets/{id}", async (string id, IPolymarketClient client, CancellationToken ct) =>
        {
            var market = await client.GetMarketAsync(id, ct);
            return market is null ? Results.NotFound() : Results.Ok(market);
        }).WithDescription("Get market by ID");

        return app;
    }
}
