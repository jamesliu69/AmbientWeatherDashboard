using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;
using static AmbientWeatherDashboard.Core.Services.OpenMeteoParser;

namespace AmbientWeatherDashboard.Core.Services;

public sealed class GeocodingService(HttpClient client)
{
    public async Task<IReadOnlyList<LocationInfo>> SearchAsync(string query, bool traditionalChinese, CancellationToken cancellationToken)
    {
        var name = query.Trim();
        if (name.Length is < 2 or > 100)
            return [];

        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(name)}"
            + $"&count=10&format=json&language={(traditionalChinese ? "zh" : "en")}";
        var json = await OpenMeteoHttp.GetJsonAsync(client, url, cancellationToken).ConfigureAwait(false);
        if (Member(json, "error").ValueKind == JsonValueKind.True || json.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Open-Meteo returned invalid geocoding data.");

        var results = Member(json, "results");
        if (results.ValueKind != JsonValueKind.Array)
            return [];

        var locations = new List<LocationInfo>();
        foreach (var entry in results.EnumerateArray())
        {
            var city = ReadText(entry, "name");
            var timezone = ReadText(entry, "timezone");
            if (city is null || timezone is null || Number(Member(entry, "latitude"), -90, 90) is not { } latitude
                || Number(Member(entry, "longitude"), -180, 180) is not { } longitude)
                continue;

            var location = new LocationInfo(city, ReadText(entry, "admin1"), ReadText(entry, "country"),
                latitude, longitude, timezone);
            if (location.IsValid)
                locations.Add(location);
        }

        return locations.DistinctBy(location => location.CoordinateKey).ToArray();
    }

    private static string? ReadText(JsonElement entry, string property)
    {
        var value = Member(entry, property);
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
