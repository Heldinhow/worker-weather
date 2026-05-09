using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polymarket.Bot.Infrastructure;
using Polymarket.Bot.Infrastructure.Persistence.DbContext;

namespace Polymarket.Bot.Api.MinimalApiExtensions;

public static class MinimalApiExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        var pgConnectionString = builder.Configuration
            .GetConnectionString("PolymarketDb")
            ?? "Host=localhost;Port=5432;Database=polymarket_bot;Username=polymarket;Password=polymarket_dev";

        var redisConnectionString = builder.Configuration
            .GetConnectionString("Redis")
            ?? "localhost:6379";

        // Infrastructure (EF Core, Redis, stub services)
        builder.Services.AddInfrastructure(pgConnectionString, redisConnectionString);

        // Health checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(pgConnectionString, name: "postgresql")
            .AddRedis(
                builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis");

        return builder;
    }

    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Auto-migrate on startup (development only)
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<PolymarketDbContext>();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex, "Database migration failed - continuing without migration");
            }
        }

        return app;
    }
}
