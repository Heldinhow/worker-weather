using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Polymarket.Bot.Api.MinimalApiExtensions;

public static class FluentResultsExtensions
{
    /// <summary>
    /// Map FluentResults to HTTP response.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsFailed)
        {
            var errors = result.Errors.Select(e => new { e.Message, e.Metadata }).ToList();
            return Results.BadRequest(new { errors });
        }

        return result.Value is null
            ? Results.NotFound()
            : Results.Ok(result.Value);
    }

    /// <summary>
    /// Map FluentResults (no value) to HTTP response.
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsFailed)
        {
            var errors = result.Errors.Select(e => new { e.Message, e.Metadata }).ToList();
            return Results.BadRequest(new { errors });
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Map nullable value to HTTP response.
    /// </summary>
    public static IResult ToHttpResult<T>(this T? value) where T : class
    {
        return value is null
            ? Results.NotFound()
            : Results.Ok(value);
    }
}
