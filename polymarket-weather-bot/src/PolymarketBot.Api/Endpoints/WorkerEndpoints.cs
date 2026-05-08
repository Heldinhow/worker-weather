using Microsoft.AspNetCore.Mvc;
using PolymarketBot.Framework.Contracts;

namespace PolymarketBot.Api.Endpoints;

public static class WorkerEndpoints
{
    public static void MapWorkerEndpoints(this WebApplication app)
    {
        app.MapGet("/workers", (IEnumerable<IWorker> workers) =>
        {
            return Results.Ok(workers.Select(w => new { w.Type, w.Status }));
        }).WithTags("Workers");

        app.MapGet("/signals", async (IWorker weatherWorker) =>
        {
            if (weatherWorker.Type != "Weather")
                return Results.NotFound();
            var signals = await weatherWorker.ScanMarketsAsync();
            return Results.Ok(signals);
        }).WithTags("Workers");

        app.MapPost("/signals/scan", async (IWorker weatherWorker) =>
        {
            var signals = await weatherWorker.ScanMarketsAsync();
            return Results.Ok(new { SignalsGenerated = signals.Count(), Timestamp = DateTime.UtcNow });
        }).WithTags("Workers");
    }
}