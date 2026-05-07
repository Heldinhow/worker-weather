using Polymarket.Bot.Api.MinimalApiExtensions;
using Polymarket.Bot.Application.Abstractions.Interfaces;
using Polymarket.Bot.Domain.Enums;

namespace Polymarket.Bot.Api.Endpoints;

public static class TradingEndpoints
{
    public static IEndpointRouteBuilder MapTradingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/positions/open", async (IPositionRepository repo, CancellationToken ct) =>
        {
            var positions = await repo.GetOpenAsync(ct);
            return Results.Ok(positions.Select(MapPosition));
        }).WithDescription("Get open positions");

        app.MapGet("/api/positions/closed", async (IPositionRepository repo, CancellationToken ct) =>
        {
            var positions = await repo.GetClosedAsync(ct);
            return Results.Ok(positions.Select(MapPosition));
        }).WithDescription("Get closed positions");

        app.MapGet("/api/orders", async (IOrderRepository repo, CancellationToken ct) =>
        {
            var orders = new List<object>(); // stub
            return Results.Ok(orders);
        }).WithDescription("Get all orders");

        app.MapGet("/api/trades", async (ITradeRepository repo, CancellationToken ct) =>
        {
            var trades = await repo.GetOpenAsync(ct);
            return Results.Ok(trades.Select(t => new
            {
                t.Id,
                t.MarketTicker,
                t.Side,
                t.EntryPrice,
                t.Size,
                t.Shares,
                t.Status,
                t.Settled,
                t.Result
            }));
        }).WithDescription("Get all trades");

        return app;
    }

    private static object MapPosition(Domain.Entities.Position p) => new
    {
        p.Id,
        p.MarketTicker,
        p.Side,
        p.EntryPrice,
        p.StopLossPrice,
        p.CurrentPrice,
        p.Shares,
        p.Size,
        p.Status,
        RealizedPnl = p.RealizedPnl.Value,
        UnrealizedPnl = p.UnrealizedPnl.Value,
        p.TotalPnl,
        p.OpenedAt,
        p.ClosedAt,
        p.ExitReason
    };
}
