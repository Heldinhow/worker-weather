using PolymarketBot.Workers.Weather.Domain.Entities;

namespace PolymarketBot.Workers.Weather.Application.Services;

public interface IWeatherForecastService
{
    Task<WeatherForecast> GetForecastAsync(string city, DateOnly date);
}