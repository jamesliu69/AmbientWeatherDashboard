namespace AmbientWeatherDashboard.Core.Models;

/// <summary>保存單一時刻的溫度、濕度、露點、天氣代碼、紫外線、輻射與風速預報。</summary>
public sealed record HourlyForecast(DateTimeOffset Time, double? Temperature, double? Humidity, double? DewPoint,
    int? Code, bool? IsDay, double? UvIndex, double? Radiation, double? WindSpeed = null);
