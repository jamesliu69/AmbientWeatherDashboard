using System.Text.Json;

namespace AmbientWeatherDashboard.Tests;

/// <summary>建立解析器及更新服務測試共用的 Open-Meteo JSON 資料。</summary>
internal static class ForecastFixture
{
    /// <summary>測試解析器使用的固定 UTC 擷取時間，回應中的觀測時間預設早 14 分鐘。</summary>
    internal static readonly DateTimeOffset Now = new(2026, 2, 5, 11, 14, 0, TimeSpan.Zero);
    /// <summary>臺北當地 2026 年 2 月 5 日午夜所對應的 UTC 時刻。</summary>
    internal static readonly DateTimeOffset Midnight = new(2026, 2, 4, 16, 0, 0, TimeSpan.Zero);

    /// <summary>建立含目前、逐時及逐日資料的回應 JSON，可覆寫濕度與目前時間。</summary>
    internal static string Json(string humidity = "70", long? currentTime = null)
    {
        var midnight = Midnight.ToUnixTimeSeconds();
        var sample = Now.AddMinutes(-14).ToUnixTimeSeconds();
        return $$"""
            {
              "timezone": "Asia/Taipei", "utc_offset_seconds": 28800,
              "current": {"time": {{currentTime ?? sample}}, "temperature_2m": 23, "relative_humidity_2m": {{humidity}},
                "apparent_temperature": 24, "weather_code": 2, "is_day": 0, "wind_speed_10m": 19.3,
                "wind_direction_10m": 360, "wind_gusts_10m": 23.5},
              "hourly": {"time": [{{sample - 86400}}, {{sample}}, {{sample + 3600}}, null, {{sample + 10800}}],
                "temperature_2m": [22, 23, 24, 25, 26], "relative_humidity_2m": [52, 70],
                "dew_point_2m": [13, 17], "weather_code": [2, 2, 3, 0, 999],
                "is_day": [0, 0, 0], "uv_index": [0.2, 0.3], "shortwave_radiation": [5, 6], "wind_speed_10m": [10, 11, 41]},
              "daily": {"time": [{{midnight - 86400}}, {{midnight}}],
                "sunrise": [null, {{midnight + 25200}}],
                "sunset": [null, {{midnight + 64800}}],
                "daylight_duration": [null, 39600], "temperature_2m_max": [27, 25], "uv_index_max": [3, 4.5]}
            }
            """;
    }

    /// <summary>解析提供或預設的 JSON，複製根元素以供文件釋放後使用。</summary>
    internal static JsonElement Element(string? source = null)
    {
        using var document = JsonDocument.Parse(source ?? Json());
        return document.RootElement.Clone();
    }
}
