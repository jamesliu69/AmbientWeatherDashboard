namespace AmbientWeatherDashboard.Core.Models;

public sealed record HourlyForecast(DateTimeOffset Time, double? Temperature, double? Humidity, double? DewPoint,
    int? Code, bool? IsDay, double? UvIndex, double? Radiation, double? WindSpeed = null);
