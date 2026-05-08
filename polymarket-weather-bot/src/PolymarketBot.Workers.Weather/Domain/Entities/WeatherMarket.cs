using PolymarketBot.Framework.Domain.Entities;
using PolymarketBot.Framework.Domain.ValueObjects;

namespace PolymarketBot.Workers.Weather.Domain.Entities;

public class WeatherMarket
{
    public string Id { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public Price YesPrice { get; set; } = null!;
    public Price NoPrice { get; set; } = null!;
    public decimal Volume { get; set; }
    public decimal Spread { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsResolved { get; set; }
    public string? Result { get; set; }
    public string City { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public decimal ThresholdF { get; set; }
}
