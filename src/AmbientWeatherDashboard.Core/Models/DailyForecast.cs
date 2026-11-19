namespace AmbientWeatherDashboard.Core.Models;

/// <summary>保存單日最高溫、紫外線指數、日出日落及日照時長預報。</summary>
public sealed record DailyForecast(DateOnly Date, DateTimeOffset? Sunrise, DateTimeOffset? Sunset,
    double? DaylightSeconds, double? HighTemperature, double? MaxUv);
