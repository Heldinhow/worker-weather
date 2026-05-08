using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PolymarketBot.Workers.Weather.Application.Services;
using PolymarketBot.Workers.Weather.Domain.Entities;

namespace PolymarketBot.Workers.Weather.Infrastructure.External;

public class OpenMeteoClient : IWeatherForecastService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoClient> _logger;
    private readonly Dictionary<string, (double lat, double lon)> _cityCoords;

    public OpenMeteoClient(HttpClient httpClient, ILogger<OpenMeteoClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cityCoords = new Dictionary<string, (double lat, double lon)>
        {
            ["nyc"] = (40.7128, -74.0060),
            ["chicago"] = (41.8781, -87.6298),
            ["miami"] = (25.7617, -80.1918),
            ["losangeles"] = (34.0522, -118.2437),
            ["denver"] = (39.7392, -104.9903),
            ["london"] = (51.5074, -0.1278),
            ["tokyo"] = (35.6762, 139.6503),
            ["sydney"] = (-33.8688, 151.2093),
            ["saopaulo"] = (-23.5505, -46.6333)
        };
    }

    public async Task<WeatherForecast> GetForecastAsync(string city, DateOnly date)
    {
        if (!_cityCoords.TryGetValue(city.ToLower(), out var coords))
            throw new ArgumentException($"Unknown city: {city}");

        var url = $"https://api.open-meteo.com/v1/ensemble?latitude={coords.lat}&longitude={coords.lon}";

        var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);
        if (response == null)
            throw new Exception("Failed to fetch weather data");

        var ensembleMembers = response.WeatherVars.Values
            .SelectMany(v => v.Values)
            .ToList();

        // Calculate probability based on members above threshold
        var thresholdF = 85m; // Default threshold
        var membersAbove = ensembleMembers.Count(v => v >= thresholdF);
        var probability = (decimal)membersAbove / ensembleMembers.Count;

        // Calculate z-score
        var mean = (double)ensembleMembers.Average();
        var stdDev = Math.Sqrt(ensembleMembers.Select(v => Math.Pow((double)v - mean, 2)).Average());
        var zScore = stdDev > 0 ? (decimal)((double)thresholdF - mean) / (decimal)stdDev : 0;

        return new WeatherForecast
        {
            City = city,
            Date = date,
            ThresholdF = thresholdF,
            Probability = probability,
            ZScore = zScore,
            EnsembleMembers = ensembleMembers,
            FetchedAt = DateTime.UtcNow
        };
    }
}

public class OpenMeteoResponse
{
    public WeatherVarsContainer WeatherVars { get; set; } = new();
}

public class WeatherVarsContainer
{
    public List<WeatherVariable> Values { get; set; } = new();
}

public class WeatherVariable
{
    public List<decimal> Values { get; set; } = new();
}
