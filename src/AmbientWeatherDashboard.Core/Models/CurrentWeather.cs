namespace AmbientWeatherDashboard.Core.Models;

/// <summary>保存目前觀測時間、溫度、濕度、天氣代碼與風況等資料。</summary>
public sealed record CurrentWeather(DateTimeOffset Time, double? Temperature, double? Humidity, double? FeelsLike,
    int? Code, bool? IsDay, double? WindSpeed, double? WindDirection, double? Gust);
