using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>依地點儲存並載入 Open-Meteo 天氣回應快取。</summary>
public sealed class WeatherCacheService(string appDataDirectory)
{
    /// <summary>載入指定地點的快取，確認座標相符後解析快照；無效快取回傳 null。</summary>
    public async Task<WeatherSnapshot?> LoadAsync(LocationInfo location, CancellationToken cancellationToken)
    {
        var stored = await AppFiles.ReadAsync<CacheEntry>(PathFor(location), cancellationToken).ConfigureAwait(false);
        if (stored is null)
            return null;
        try
        {
            if (stored.Coordinates != location.CoordinateKey)
                throw new InvalidDataException("Weather cache contains a different location.");

            return OpenMeteoParser.Parse(stored.Source, stored.FetchedAt);
        }
        catch (InvalidDataException ex)
        {
            Trace.TraceWarning("Weather cache rejected: {0}", ex.Message);
            return null;
        }
    }

    /// <summary>將地點座標鍵、擷取時間及原始 JSON 回應寫入快取。</summary>
    public Task SaveAsync(LocationInfo location, WeatherPayload weather, CancellationToken cancellationToken) =>
        AppFiles.WriteAsync(PathFor(location), new CacheEntry(location.CoordinateKey, weather.FetchedAt, weather.Source),
            cancellationToken);

    /// <summary>以地點座標鍵的 SHA-256 摘要組合快取檔案路徑。</summary>
    private string PathFor(LocationInfo location)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(location.CoordinateKey));
        return Path.Combine(appDataDirectory, "weather-cache", Convert.ToHexString(hash) + ".json");
    }

    /// <summary>保存快取對應的座標鍵、擷取時間與原始 JSON 回應。</summary>
    private sealed record CacheEntry(string Coordinates, DateTimeOffset FetchedAt, JsonElement Source);
}
