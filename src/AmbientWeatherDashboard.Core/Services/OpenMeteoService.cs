using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>向 Open-Meteo 天氣 API 擷取指定地點的目前天氣與預報資料。</summary>
public sealed class OpenMeteoService(HttpClient client, TimeProvider clock)
{
    /// <summary>驗證地點後下載 API JSON，並連同擷取時間與解析結果回傳。</summary>
    public async Task<WeatherPayload> GetAsync(LocationInfo location, CancellationToken cancellationToken)
    {
        if (!location.IsValid)
            throw new ArgumentException("Invalid forecast location.", nameof(location));

        var url = "https://api.open-meteo.com/v1/forecast?"
            + FormattableString.Invariant($"latitude={location.Latitude:R}&longitude={location.Longitude:R}")
            + "&timezone=auto&timeformat=unixtime&past_days=2&forecast_days=2"
            + "&temperature_unit=celsius&wind_speed_unit=kmh"
            + "&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,is_day,wind_speed_10m,wind_direction_10m,wind_gusts_10m"
            + "&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,weather_code,is_day,uv_index,shortwave_radiation,wind_speed_10m"
            + "&daily=sunrise,sunset,daylight_duration,temperature_2m_max,uv_index_max";

        var json = await OpenMeteoHttp.GetJsonAsync(client, url, cancellationToken).ConfigureAwait(false);
        var fetchedAt = clock.GetUtcNow();
        return new WeatherPayload(fetchedAt, json, OpenMeteoParser.Parse(json, fetchedAt));
    }
}
