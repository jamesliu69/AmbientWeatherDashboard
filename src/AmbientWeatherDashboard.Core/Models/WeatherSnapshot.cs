namespace AmbientWeatherDashboard.Core.Models;

public sealed record WeatherSnapshot(DateTimeOffset FetchedAt, TimeZoneInfo Zone, CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly, IReadOnlyList<DailyForecast> Daily);
