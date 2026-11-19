using System.Diagnostics;
using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

public sealed class WeatherRefreshService(OpenMeteoService weather, WeatherCacheService cache)
{
    private readonly object sync = new();
    private Task<RefreshResult>? pending;
    private string? pendingLocation;

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
