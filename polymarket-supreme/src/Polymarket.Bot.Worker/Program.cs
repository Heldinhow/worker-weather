using Serilog;
using Polymarket.Bot.Infrastructure;
using Polymarket.Bot.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog();

var pgConnectionString = builder.Configuration
    .GetConnectionString("PolymarketDb")
    ?? "Host=localhost;Port=5432;Database=polymarket_bot;Username=polymarket;Password=polymarket_dev";

var redisConnectionString = builder.Configuration
    .GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddInfrastructure(pgConnectionString, redisConnectionString);
builder.Services.AddHostedService<MarketScannerWorker>();
builder.Services.AddHostedService<ConsensusTraderWorker>();

Log.Information("Polymarket.Bot Worker starting...");
var host = builder.Build();
host.Run();
