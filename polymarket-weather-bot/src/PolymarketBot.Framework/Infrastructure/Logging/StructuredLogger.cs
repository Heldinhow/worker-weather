using Microsoft.Extensions.Logging;

namespace PolymarketBot.Framework.Infrastructure.Logging;

public interface IStructuredLogger
{
    void LogSignalGenerated(string city, string metric, decimal thresholdF, decimal modelProb, decimal marketPrice, decimal edge, decimal confidence, decimal zScore, int horizonDays, decimal spread, decimal kellySize, string action);
    void LogPositionClosed(string city, string date, string direction, decimal entryPrice, decimal exitPrice, decimal shares, decimal pnl, string closeReason, int hoursToResolution);
    void LogMarketResolved(string marketId, string city, decimal actualTempF, decimal forecastAtEntry, decimal forecastError, string result);
}

public class StructuredLogger : IStructuredLogger
{
    private readonly ILogger<StructuredLogger> _logger;

    public StructuredLogger(ILogger<StructuredLogger> logger) => _logger = logger;

    public void LogSignalGenerated(string city, string metric, decimal thresholdF, decimal modelProb, decimal marketPrice, decimal edge, decimal confidence, decimal zScore, int horizonDays, decimal spread, decimal kellySize, string action)
    {
        _logger.LogInformation("Signal generated: {Event} {City} {Metric} {ThresholdF} {ModelProb} {MarketPrice} {Edge} {Confidence} {ZScore} {HorizonDays} {Spread} {KellySize} {Action}",
            "signal_generated", city, metric, thresholdF, modelProb, marketPrice, edge, confidence, zScore, horizonDays, spread, kellySize, action);
    }

    public void LogPositionClosed(string city, string date, string direction, decimal entryPrice, decimal exitPrice, decimal shares, decimal pnl, string closeReason, int hoursToResolution)
    {
        _logger.LogInformation("Position closed: {Event} {City} {Date} {Direction} {EntryPrice} {ExitPrice} {Shares} {Pnl} {CloseReason} {HoursToResolution}",
            "position_closed", city, date, direction, entryPrice, exitPrice, shares, pnl, closeReason, hoursToResolution);
    }

    public void LogMarketResolved(string marketId, string city, decimal actualTempF, decimal forecastAtEntry, decimal forecastError, string result)
    {
        _logger.LogInformation("Market resolved: {Event} {MarketId} {City} {ActualTempF} {ForecastAtEntry} {ForecastError} {Result}",
            "market_resolved", marketId, city, actualTempF, forecastAtEntry, forecastError, result);
    }
}