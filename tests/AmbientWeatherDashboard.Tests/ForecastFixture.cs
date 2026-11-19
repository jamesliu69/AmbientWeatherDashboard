using System.Text.Json;

namespace AmbientWeatherDashboard.Tests;

internal static class ForecastFixture
{
    internal static readonly DateTimeOffset Now = new(2026, 2, 5, 11, 14, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset Midnight = new(2026, 2, 4, 16, 0, 0, TimeSpan.Zero);

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

    internal static JsonElement Element(string? source = null)
    {
        using var document = JsonDocument.Parse(source ?? Json());
        return document.RootElement.Clone();
    }
}
