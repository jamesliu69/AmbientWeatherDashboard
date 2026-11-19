using System.Globalization;
using System.Text.Json.Serialization;

namespace AmbientWeatherDashboard.Core.Models;

/// <summary>保存地點名稱、經緯度與時區，並提供驗證及顯示所需的衍生值。</summary>
public sealed record LocationInfo(string Name, string? Region, string? Country, double Latitude, double Longitude, string TimeZone)
{
    /// <summary>提供臺北的預設地點資料。</summary>
    public static readonly LocationInfo Taipei = new("Taipei", null, "Taiwan", 25.033, 121.5654, "Asia/Taipei");

    /// <summary>檢查名稱、座標範圍及時區識別碼是否有效。</summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && Latitude is >= -90 and <= 90
        && Longitude is >= -180 and <= 180 && WeatherMath.FindZone(TimeZone) is not null;

    /// <summary>以逗號串接不重複且非空的地點名稱、地區與國家。</summary>
    [JsonIgnore]
    public string Label => string.Join(", ", new[] { Name, Region, Country }
        .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>以固定不受文化設定影響的格式組合緯度與經度。</summary>
    [JsonIgnore]
    public string CoordinateKey => string.Create(CultureInfo.InvariantCulture, $"{Latitude:R},{Longitude:R}");
}
