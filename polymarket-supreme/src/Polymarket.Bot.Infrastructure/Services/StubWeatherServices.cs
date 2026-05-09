using Polymarket.Bot.Application.Abstractions.Interfaces;

namespace Polymarket.Bot.Infrastructure.Services;

public class OpenWeatherMapStub : IWeatherService
{
    public string ServiceName => "OpenWeatherMap";

    public async Task<WeatherDataDto?> GetWeatherAsync(string city, CancellationToken ct = default)
    {
        var cityHash = city.ToLower().GetHashCode();
        var rng = new Random(cityHash * 42);
        var rainProb = 65.0 + (rng.NextDouble() * 30.0);
        var temp = 15.0 + (rng.NextDouble() * 15.0);

        return await Task.FromResult((WeatherDataDto?)new WeatherDataDto(
            city,
            ServiceName,
            rainProb > 70 ? "rain" : "clear",
            Math.Round(rainProb, 1),
            Math.Round(temp, 1),
            DateTime.UtcNow));
    }
}

public class WeatherApiStub : IWeatherService
{
    public string ServiceName => "WeatherAPI";

    public async Task<WeatherDataDto?> GetWeatherAsync(string city, CancellationToken ct = default)
    {
        var cityHash = city.ToLower().GetHashCode();
        var rng = new Random(cityHash * 73);
        var rainProb = 62.0 + (rng.NextDouble() * 32.0);
        var temp = 16.0 + (rng.NextDouble() * 14.0);

        return await Task.FromResult((WeatherDataDto?)new WeatherDataDto(
            city,
            ServiceName,
            rainProb > 70 ? "rain" : "clear",
            Math.Round(rainProb, 1),
            Math.Round(temp, 1),
            DateTime.UtcNow));
    }
}
