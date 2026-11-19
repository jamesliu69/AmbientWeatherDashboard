using System.Globalization;
using System.Text.Json.Serialization;

namespace AmbientWeatherDashboard.Core.Models;

public sealed record LocationInfo(string Name, string? Region, string? Country, double Latitude, double Longitude, string TimeZone)
{
    public static readonly LocationInfo Taipei = new("Taipei", null, "Taiwan", 25.033, 121.5654, "Asia/Taipei");

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && Latitude is >= -90 and <= 90
        && Longitude is >= -180 and <= 180 && WeatherMath.FindZone(TimeZone) is not null;

    [JsonIgnore]
    public string Label => string.Join(", ", new[] { Name, Region, Country }
        .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));

    [JsonIgnore]
    public string CoordinateKey => string.Create(CultureInfo.InvariantCulture, $"{Latitude:R},{Longitude:R}");
}
