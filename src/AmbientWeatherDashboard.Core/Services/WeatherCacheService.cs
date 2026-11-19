using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

public sealed class WeatherCacheService(string appDataDirectory)
{
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

    public Task SaveAsync(LocationInfo location, WeatherPayload weather, CancellationToken cancellationToken) =>
        AppFiles.WriteAsync(PathFor(location), new CacheEntry(location.CoordinateKey, weather.FetchedAt, weather.Source),
            cancellationToken);

    private string PathFor(LocationInfo location)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(location.CoordinateKey));
        return Path.Combine(appDataDirectory, "weather-cache", Convert.ToHexString(hash) + ".json");
    }

    private sealed record CacheEntry(string Coordinates, DateTimeOffset FetchedAt, JsonElement Source);
}
