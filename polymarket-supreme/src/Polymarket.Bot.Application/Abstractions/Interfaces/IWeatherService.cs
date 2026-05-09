namespace Polymarket.Bot.Application.Abstractions.Interfaces;

public interface IWeatherService
{
    string ServiceName { get; }
    Task<WeatherDataDto?> GetWeatherAsync(string city, CancellationToken ct = default);
}

public record WeatherDataDto(
    string City,
    string ServiceName,
    string Condition,
    double RainProbability,
    double Temperature,
    DateTime FetchedAt);
