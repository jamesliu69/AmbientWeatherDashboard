namespace AmbientWeatherDashboard.Core.Models;

/// <summary>保存取得時間、地點時區、目前天氣與逐時及逐日預報。</summary>
public sealed record WeatherSnapshot(DateTimeOffset FetchedAt, TimeZoneInfo Zone, CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly, IReadOnlyList<DailyForecast> Daily);
