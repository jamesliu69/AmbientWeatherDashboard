using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>將 Open-Meteo JSON 回應驗證並轉換為目前天氣與逐時、逐日預報模型。</summary>
public static class OpenMeteoParser
{
    /// <summary>解析時區、目前天氣及預報序列，並拒絕不符合資料範圍的必要欄位。</summary>
    public static WeatherSnapshot Parse(JsonElement root, DateTimeOffset fetchedAt)
    {
        var timezone = Member(root, "timezone");
        var zone = WeatherMath.FindZone(timezone.ValueKind == JsonValueKind.String ? timezone.GetString() : null)
            ?? throw new InvalidDataException("Open-Meteo returned an invalid time zone.");
        var offsetSeconds = Number(Member(root, "utc_offset_seconds"), -50400, 50400)
            ?? throw new InvalidDataException("Open-Meteo omitted the daily UTC offset.");
        if (offsetSeconds % 60 != 0)
            throw new InvalidDataException("Open-Meteo returned an unsupported daily UTC offset.");
        var offset = TimeSpan.FromSeconds(offsetSeconds);

        var current = Member(root, "current");
        var time = Timestamp(Member(current, "time")) ?? throw new InvalidDataException("Current weather has no valid time.");
        if (Math.Abs((fetchedAt - time).TotalMinutes) > 90)
            throw new InvalidDataException("Current weather timestamp is outside the refresh window.");

        var weather = new CurrentWeather(time,
            Read(current, "temperature_2m", -100, 70),
            Read(current, "relative_humidity_2m", 0, 100),
            Read(current, "apparent_temperature", -130, 100),
            Code(Member(current, "weather_code")), Day(Member(current, "is_day")),
            Read(current, "wind_speed_10m", 0, 500),
            Direction(Read(current, "wind_direction_10m", 0, 360)),
            Read(current, "wind_gusts_10m", 0, 500));
        if (weather.Temperature is null && weather.Humidity is null && weather.Code is null
            && weather.FeelsLike is null && weather.WindSpeed is null && weather.WindDirection is null && weather.Gust is null)
            throw new InvalidDataException("Current weather contains no usable measurements.");

        var hourly = Member(root, "hourly");
        var hours = Series(hourly, (index, timestamp) => new HourlyForecast(HourlySampleTime(timestamp, offset),
            Read(hourly, "temperature_2m", -100, 70, index),
            Read(hourly, "relative_humidity_2m", 0, 100, index),
            Read(hourly, "dew_point_2m", -120, 70, index),
            Code(At(hourly, "weather_code", index)), Day(At(hourly, "is_day", index)),
            Read(hourly, "uv_index", 0, 100, index),
            Read(hourly, "shortwave_radiation", 0, 1800, index),
            Read(hourly, "wind_speed_10m", 0, 500, index)))
            .DistinctBy(hour => hour.Time).OrderBy(hour => hour.Time).ToArray();

        var daily = Member(root, "daily");
        var days = Series(daily, (index, timestamp) =>
        {
            // Open-Meteo uses the response offset for its daily row boundaries.
            // Open-Meteo 的每日資料列邊界依回應所帶的 UTC 偏移量換算。
            var date = DateOnly.FromDateTime(timestamp.ToOffset(offset).DateTime);
            var highTemperature = Read(daily, "temperature_2m_max", -100, 70, index);
            var maxUv = Read(daily, "uv_index_max", 0, 100, index);
            if (WeatherMath.Local(timestamp, zone).TimeOfDay != TimeSpan.Zero
                || WeatherMath.Local(timestamp.AddDays(1), zone).TimeOfDay != TimeSpan.Zero)
                (highTemperature, maxUv) = LocalDayPeaks(date, zone, hours);

            return new DailyForecast(date,
                SolarTimestamp(At(daily, "sunrise", index), timestamp),
                SolarTimestamp(At(daily, "sunset", index), timestamp),
                Read(daily, "daylight_duration", 0, 86400, index),
                highTemperature, maxUv);
        });

        return new WeatherSnapshot(fetchedAt, zone, weather, hours,
            days.DistinctBy(day => day.Date).OrderBy(day => day.Date).ToArray());
    }

    /// <summary>以完整連續的本地日小時資料計算最高溫與最高紫外線指數，樣本不足時回傳空結果。</summary>
    private static (double? Temperature, double? Uv) LocalDayPeaks(DateOnly date, TimeZoneInfo zone, HourlyForecast[] hours)
    {
        var samples = hours.Where(hour => DateOnly.FromDateTime(WeatherMath.Local(hour.Time, zone).DateTime) == date).ToArray();
        if (samples.Length == 0 || samples[^1].Time - samples[0].Time != TimeSpan.FromHours(samples.Length - 1)
            || !hours.Any(hour => hour.Time == samples[0].Time.AddHours(-1))
            || !hours.Any(hour => hour.Time == samples[^1].Time.AddHours(1)))
            return (null, null);

        return (samples.All(hour => hour.Temperature.HasValue) ? samples.Max(hour => hour.Temperature) : null,
            samples.All(hour => hour.UvIndex.HasValue) ? samples.Max(hour => hour.UvIndex) : null);
    }

    /// <summary>依 time 陣列的索引解析序列項目，略過無效時間戳記。</summary>
    private static List<T> Series<T>(JsonElement series, Func<int, DateTimeOffset, T> parse)
    {
        var rows = new List<T>();
        var times = Member(series, "time");
        if (times.ValueKind != JsonValueKind.Array)
            return rows;

        var index = 0;
        foreach (var item in times.EnumerateArray())
        {
            if (Timestamp(item) is { } timestamp)
                rows.Add(parse(index, timestamp));
            index++;
        }
        return rows;
    }

    /// <summary>從 JSON 物件取得指定欄位；物件或欄位不存在時回傳未定義值。</summary>
    internal static JsonElement Member(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item) ? item : default;

    /// <summary>從指定欄位的陣列取得索引項目，欄位不是陣列或索引超出範圍時回傳未定義值。</summary>
    private static JsonElement At(JsonElement value, string key, int index)
    {
        var series = Member(value, key);
        return series.ValueKind == JsonValueKind.Array && index < series.GetArrayLength() ? series[index] : default;
    }

    /// <summary>讀取 JSON 數值欄位或序列項目，並依給定上下限驗證。</summary>
    private static double? Read(JsonElement value, string key, double min, double max, int? index = null) =>
        Number(index is { } i ? At(value, key, i) : Member(value, key), min, max);

    /// <summary>讀取有限且落在指定範圍內的 JSON 數值，其他值回傳 null。</summary>
    internal static double? Number(JsonElement value, double min, double max) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number)
        && number >= min && number <= max ? number : null;

    /// <summary>將有效風向角度正規化至 0 度以上且小於 360 度。</summary>
    private static double? Direction(double? degrees) => degrees is { } value ? WeatherMath.NormalizeDegrees(value) : null;

    /// <summary>只接受程式列出的天氣代碼，無效或未支援的值回傳 null。</summary>
    private static int? Code(JsonElement element) => element.ValueKind == JsonValueKind.Number
        && element.TryGetInt32(out var code) && code is 0 or 1 or 2 or 3 or 45 or 48
        or 51 or 53 or 55 or 56 or 57 or 61 or 63 or 65 or 66 or 67 or 71 or 73 or 75 or 77
        or 80 or 81 or 82 or 85 or 86 or 95 or 96 or 99 ? code : null;

    /// <summary>將 0 或 1 解析為夜間或白天，其餘值回傳 null。</summary>
    private static bool? Day(JsonElement element) => Number(element, 0, 1) switch { 0 => false, 1 => true, _ => null };

    /// <summary>將支援範圍內的 Unix 秒數解析為時間，其他值回傳 null。</summary>
    private static DateTimeOffset? Timestamp(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var seconds)
        && seconds is >= 0 and <= 7258118400 ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;

    /// <summary>依時區 UTC 偏移的小時內部分校正逐時資料時間戳記，無法對齊整點時保留原時間。</summary>
    private static DateTimeOffset HourlySampleTime(DateTimeOffset timestamp, TimeSpan offset)
    {
        // shortcut: Open-Meteo #2163 labels UTC model hours with a fractional shift, remove when the API aligns its labels.
        // 此修正只在加入偏移後能對齊整點時生效，否則回傳原時間。
        var corrected = timestamp.AddTicks(offset.Ticks % TimeSpan.TicksPerHour);
        return corrected.Ticks % TimeSpan.TicksPerHour == 0 ? corrected : timestamp;
    }

    /// <summary>解析落在每日資料列前一天至後兩天範圍內的日出或日落時間。</summary>
    private static DateTimeOffset? SolarTimestamp(JsonElement value, DateTimeOffset boundary) =>
        Timestamp(value) is { } time && time >= boundary.AddDays(-1) && time <= boundary.AddDays(2) ? time : null;
}
