using Polymarket.Bot.Api.MinimalApiExtensions;
using Polymarket.Bot.Application.Abstractions.Interfaces;

namespace Polymarket.Bot.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/summary", () =>
        {
            return Results.Ok(new
            {
                totalTrades = 0,
                winningTrades = 0,
                losingTrades = 0,
                winRate = 0.0,
                totalPnl = 0.0,
                averagePnl = 0.0,
                openPositions = 0,
                closedPositions = 0,
                pendingTrades = 0
            });
        }).WithDescription("Get analytics summary");

        app.MapGet("/api/analytics/win-rate", () =>
        {
            return Results.Ok(new
            {
                total = 0,
                wins = 0,
                losses = 0,
                winRate = 0.0,
                totalPnl = 0.0
            });
        }).WithDescription("Get overall win rate");

        app.MapGet("/api/analytics/win-rate/by-strategy", () =>
        {
            return Results.Ok(new List<object>());
        }).WithDescription("Get win rate by strategy");

        app.MapGet("/api/analytics/win-rate/by-trend", () =>
        {
            return Results.Ok(new List<object>());
        }).WithDescription("Get win rate by trend direction");

        app.MapGet("/api/analytics/win-rate/by-momentum", () =>
        {
            return Results.Ok(new List<object>());
        }).WithDescription("Get win rate by momentum state");

        app.MapGet("/api/analytics/self-improvement-report", () =>
        {
            return Results.Ok(new
            {
                generatedAt = DateTime.UtcNow,
                totalTrades = 0,
                winRate = 0.0,
                totalPnl = 0.0,
                topStrategies = new List<string>(),
                topFactors = new List<string>(),
                worstFactors = new List<string>(),
                recommendations = new List<string>(),
                warnings = new List<string> { "Insufficient data for self-improvement report" }
            });
        }).WithDescription("Get self-improvement report");

        app.MapGet("/api/decision-traces", async (IDecisionTraceRepository repo, CancellationToken ct) =>
        {
            var traces = await repo.GetByTypeAsync("trade", 100, ct);
            return Results.Ok(traces.Select(t => new
            {
                t.Id,
                t.ExecutionCycleId,
                t.TraceType,
                t.Status,
                t.CreatedAt,
                t.CompletedAt
            }));
        }).WithDescription("Get decision traces");

        return app;
    }
}
