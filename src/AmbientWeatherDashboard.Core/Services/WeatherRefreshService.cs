using System.Diagnostics;
using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>協調相同地點的共用更新請求，並在天氣更新失敗時載入快取資料。</summary>
public sealed class WeatherRefreshService(OpenMeteoService weather, WeatherCacheService cache)
{
    /// <summary>保護待處理更新工作與其地點鍵的共用狀態。</summary>
    private readonly object sync = new();
    /// <summary>目前共用中的天氣更新工作。</summary>
    private Task<RefreshResult>? pending;
    /// <summary>目前共用更新工作的地點座標鍵。</summary>
    private string? pendingLocation;

    /// <summary>同地點呼叫共用更新工作；不同地點等待現有工作結束後再發出更新。</summary>
    public async Task<RefreshResult> RefreshAsync(LocationInfo location, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task<RefreshResult> request;
            bool sameLocation;
            bool ownsRequest;
            lock (sync)
            {
                ownsRequest = pending is null || pending.IsCompleted;
                if (ownsRequest)
                {
                    pendingLocation = location.CoordinateKey;
                    pending = Task.Run(() => FetchAsync(location, cancellationToken), cancellationToken);
                }

                request = pending!;
                sameLocation = pendingLocation == location.CoordinateKey;
            }

            if (ownsRequest)
                return await request.ConfigureAwait(false);
            if (sameLocation)
                return await request.WaitAsync(cancellationToken).ConfigureAwait(false);

            await Task.WhenAny(request).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>天氣資料取得失敗時載入快取；快取寫入失敗時仍回傳已取得的快照。</summary>
    private async Task<RefreshResult> FetchAsync(LocationInfo location, CancellationToken cancellationToken)
    {
        WeatherPayload payload;
        try
        {
            payload = await weather.GetAsync(location, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or TimeoutException or InvalidDataException)
        {
            Trace.TraceWarning("Weather refresh failed: {0}", ex.Message);
            var cached = await cache.LoadAsync(location, cancellationToken).ConfigureAwait(false);
            return new RefreshResult(cached, false, ex.Message);
        }

        try
        {
            await cache.SaveAsync(location, payload, cancellationToken).ConfigureAwait(false);
            return new RefreshResult(payload.Snapshot, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("Unable to save weather cache: {0}", ex.Message);
            return new RefreshResult(payload.Snapshot, true, ex.Message, CacheWriteFailed: true);
        }
    }
}
