using Serilog;
using Polymarket.Bot.Api.Endpoints;
using Polymarket.Bot.Api.MinimalApiExtensions;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Polymarket.Bot API", Version = "v1" });
});

// Application + Infrastructure services
builder.AddApplicationServices();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Polymarket.Bot API v1"));

// Auto-migrate (development)
app.UseApplicationMiddleware();

// Health check
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
        });
    }
}).WithTags("Health");

// API endpoints
app.MapHealthEndpoints();
app.MapMarketsEndpoints();
app.MapTradingEndpoints();
app.MapAnalyticsEndpoints();

Log.Information("Polymarket.Bot API starting on {Urls}", string.Join(", ", app.Urls));
app.Run();
