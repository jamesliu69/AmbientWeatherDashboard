namespace AmbientWeatherDashboard.Core.Models;

public sealed record DailyForecast(DateOnly Date, DateTimeOffset? Sunrise, DateTimeOffset? Sunset,
    double? DaylightSeconds, double? HighTemperature, double? MaxUv);
