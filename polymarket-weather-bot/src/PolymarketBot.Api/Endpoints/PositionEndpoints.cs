using Microsoft.AspNetCore.Mvc;
using PolymarketBot.Framework.Application.Services;

namespace PolymarketBot.Api.Endpoints;

public static class PositionEndpoints
{
    public static void MapPositionEndpoints(this WebApplication app)
    {
        app.MapGet("/positions", async (IPositionService positionService) =>
        {
            var positions = await positionService.GetOpenPositionsAsync();
            return Results.Ok(positions);
        }).WithTags("Positions");

        app.MapGet("/positions/{id}", async (Guid id, IPositionService positionService) =>
        {
            var positions = await positionService.GetOpenPositionsAsync();
            var position = positions.FirstOrDefault(p => p.Id == id);
            return position == null ? Results.NotFound() : Results.Ok(position);
        }).WithTags("Positions");

        app.MapGet("/positions/{id}/close", async (Guid id, IPositionService positionService) =>
        {
            var result = await positionService.ClosePositionAsync(id);
            return result.IsFailed ? Results.BadRequest(result.Errors) : Results.Ok(result.Value);
        }).WithTags("Positions");
    }
}