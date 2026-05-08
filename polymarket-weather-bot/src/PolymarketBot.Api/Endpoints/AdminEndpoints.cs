using Microsoft.AspNetCore.Mvc;

namespace PolymarketBot.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/stats", () => Results.Ok(new { TotalTrades = 0, WinRate = 0.0, TotalPnL = 0.0 }))
           .WithTags("Admin");

        app.MapGet("/stats/calibration", () => Results.Ok(new { CalibrationData = new object[] { } }))
           .WithTags("Admin");

        app.MapGet("/config", () => Results.Ok(new { Config = "See config.yaml" }))
           .WithTags("Admin");

        app.MapPut("/config", (object config) => Results.Ok(new { Status = "Config updated" }))
           .WithTags("Admin");

        app.MapPost("/admin/reset", () => Results.Ok(new { Status = "Reset complete" }))
           .WithTags("Admin");
    }
}