using Serilog;
using Polymarket.Bot.Worker;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog();
builder.Services.AddHostedService<Worker>();

Log.Information("Polymarket.Bot Worker starting...");
var host = builder.Build();
host.Run();
