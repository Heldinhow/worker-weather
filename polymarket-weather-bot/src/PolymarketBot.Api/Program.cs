using PolymarketBot.Api.BackgroundServices;
using PolymarketBot.Api.Endpoints;
using PolymarketBot.Api.Middleware;
using PolymarketBot.Framework.Application.Services;
using PolymarketBot.Framework.Application.UseCases;
using PolymarketBot.Framework.Contracts;
using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Framework.Infrastructure.External;
using PolymarketBot.Framework.Infrastructure.Persistence;
using PolymarketBot.Workers.Weather;
using PolymarketBot.Workers.Weather.Application.Services;
using PolymarketBot.Workers.Weather.Application.UseCases;
using PolymarketBot.Workers.Weather.Infrastructure.External;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Framework services (simulation mode)
builder.Services.AddSingleton<IPositionService, InMemoryPositionService>();
builder.Services.AddSingleton<IPolymarketClient, PolymarketCLOBClient>();

// Weather worker services
builder.Services.AddHttpClient<IWeatherForecastService, OpenMeteoClient>();
builder.Services.AddSingleton<IMarketDataService, PolymarketMarketDataService>();
builder.Services.AddSingleton<WeatherSignalGenerator>(sp => 
    new WeatherSignalGenerator(
        sp.GetRequiredService<ILogger<WeatherSignalGenerator>>(),
        minEdgeThreshold: 0.08m,
        kellyFraction: 0.25m,
        balance: 10000m));
builder.Services.AddSingleton<IWorker, WeatherWorker>();

// Use cases
builder.Services.AddScoped<ExecuteTradeUseCase>();
builder.Services.AddScoped<MonitorPositionsUseCase>();
builder.Services.AddScoped<ResolveTradesUseCase>();
builder.Services.AddScoped<ScanMarketsUseCase>();
builder.Services.AddScoped<CalculateSignalUseCase>();

// Background services
builder.Services.AddHostedService<ScanLoopService>();
builder.Services.AddHostedService<PositionMonitorService>();
builder.Services.AddHostedService<MarketResolutionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiting();

app.MapHealthEndpoints();
app.MapMarketEndpoints();
app.MapPositionEndpoints();
app.MapWorkerEndpoints();
app.MapAdminEndpoints();

app.Run();
