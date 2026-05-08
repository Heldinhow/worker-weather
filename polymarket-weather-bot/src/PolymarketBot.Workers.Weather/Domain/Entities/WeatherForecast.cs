namespace PolymarketBot.Workers.Weather.Domain.Entities;

public class WeatherForecast
{
    public string City { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal ThresholdF { get; set; }
    public decimal Probability { get; set; }
    public decimal ZScore { get; set; }
    public List<decimal> EnsembleMembers { get; set; } = new();
    public DateTime FetchedAt { get; set; }
}
