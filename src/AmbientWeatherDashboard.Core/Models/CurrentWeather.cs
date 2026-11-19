namespace AmbientWeatherDashboard.Core.Models;

public sealed record CurrentWeather(DateTimeOffset Time, double? Temperature, double? Humidity, double? FeelsLike,
    int? Code, bool? IsDay, double? WindSpeed, double? WindDirection, double? Gust);
