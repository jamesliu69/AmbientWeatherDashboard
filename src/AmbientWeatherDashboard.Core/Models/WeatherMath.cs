using TimeZoneConverter;

namespace AmbientWeatherDashboard.Core.Models;

/// <summary>提供天氣數值換算、時區轉換、角度計算與日照週期運算。</summary>
public static class WeatherMath
{
    /// <summary>依顯示偏好將攝氏溫度換算為華氏溫度，否則回傳攝氏值。</summary>
    public static double Temperature(double celsius, bool fahrenheit) => fahrenheit ? celsius * 9 / 5 + 32 : celsius;
    /// <summary>依顯示偏好將公里每小時換算為英里每小時，否則回傳原值。</summary>
    public static double WindSpeed(double kilometersPerHour, bool mph) => mph ? kilometersPerHour / 1.609344 : kilometersPerHour;
    /// <summary>將角度正規化至 0 度以上且小於 360 度的範圍。</summary>
    public static double NormalizeDegrees(double degrees) => (degrees % 360 + 360) % 360;
    /// <summary>計算由 from 轉向 to 的最短角度，結果介於 -180 度（含）與 180 度（不含）之間。</summary>
    public static double ShortestTurn(double from, double to) => NormalizeDegrees(to - from + 180) - 180;
    /// <summary>將濕度百分比限制在 0 至 100，再換算成圓弧角度。</summary>
    public static double HumiditySweep(double percent) => Math.Clamp(percent, 0, 100) * 3.6;
    /// <summary>依紫外線指數門檻回傳 0 至 4 的級別。</summary>
    public static int UvLevel(double value) => value switch { < 3 => 0, < 6 => 1, < 8 => 2, < 11 => 3, _ => 4 };
    /// <summary>將輻射值除以 10 並限制在 0 至 100，作為光照指示值。</summary>
    public static double LightIndex(double radiation) => Math.Clamp(radiation / 10, 0, 100);

    /// <summary>先以系統時區資料查找識別碼，再使用 TimeZoneConverter 查找，無法識別時回傳 null。</summary>
    public static TimeZoneInfo? FindZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return null;
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone))
            return zone;

        return TZConvert.TryGetTimeZoneInfo(timeZone, out zone) ? zone : null;
    }

    /// <summary>將時間轉換為指定時區的日期與時間。</summary>
    public static DateTimeOffset Local(DateTimeOffset time, TimeZoneInfo zone) => TimeZoneInfo.ConvertTime(time, zone);

    /// <summary>將一天中的時間換算為正規化的圓周角度。</summary>
    public static double ClockAngle(TimeSpan wallTime) => NormalizeDegrees(wallTime.TotalHours * 15 - 180);

    /// <summary>尋找指定時區中前一天同一小時的預報，重複小時取 UTC 偏移最接近的一筆。</summary>
    public static HourlyForecast? SameHourYesterday(IEnumerable<HourlyForecast> hours, DateTimeOffset time, TimeZoneInfo zone)
    {
        var local = Local(time, zone);
        var previousDate = DateOnly.FromDateTime(local.DateTime).AddDays(-1);
        return hours.Where(hour =>
            {
                var candidate = Local(hour.Time, zone);
                return DateOnly.FromDateTime(candidate.DateTime) == previousDate && candidate.Hour == local.Hour;
            })
            .MinBy(hour => Math.Abs((Local(hour.Time, zone).Offset - local.Offset).Ticks));
    }

    /// <summary>依目前時間及相鄰日期的日出日落資料選出涵蓋該時刻的日照週期。</summary>
    public static DaylightCycle ActiveSunCycle(DateTimeOffset now, DailyForecast? today, DailyForecast? yesterday,
        DailyForecast? tomorrow, TimeZoneInfo zone)
    {
        var cycle = SunCycle(now, today, zone);
        if (cycle.Seconds == 86400 || Contains(now, cycle))
            return cycle;

        var next = SunCycle(now, tomorrow, zone);
        if (Contains(now, next))
            return next;
        var previous = SunCycle(now, yesterday, zone);
        return Contains(now, previous) ? previous : cycle;
    }

    /// <summary>判斷目前時間是否落在具有日出與日落時間的日照區間內。</summary>
    private static bool Contains(DateTimeOffset now, DaylightCycle cycle) =>
        cycle.Sunrise is { } rise && cycle.Sunset is { } set && now >= rise && now < set;

    /// <summary>依單日預報建立日照弧線，處理極晝、極夜及跨越午夜的日落時間。</summary>
    public static DaylightCycle SunCycle(DateTimeOffset now, DailyForecast? day, TimeZoneInfo zone)
    {
        if (day is null)
            return default;
        if (day.DaylightSeconds is 0)
            return new DaylightCycle(0, 0, 0, 0, null, null);
        if (day.DaylightSeconds is 86400)
        {
            var local = Local(now, zone);
            return new DaylightCycle(180, 360, local.TimeOfDay.TotalDays, 86400, null, null);
        }

        var rise = day.Sunrise;
        var set = day.Sunset;
        if (rise is null || set is null || rise == set)
            return new DaylightCycle(0, 0, null, day.DaylightSeconds, rise == set ? null : rise, rise == set ? null : set);

        if (set < rise)
        {
            var next = Local(set.Value, zone).DateTime.AddDays(1);
            if (zone.IsInvalidTime(next))
                return new DaylightCycle(0, 0, null, day.DaylightSeconds, rise, null);
            set = new DateTimeOffset(next, zone.GetUtcOffset(next));
        }

        var span = set.Value - rise.Value;
        if (span <= TimeSpan.Zero || span > TimeSpan.FromHours(25))
            return new DaylightCycle(0, 0, null, day.DaylightSeconds, null, null);

        var start = ClockAngle(Local(rise.Value, zone).TimeOfDay);
        var end = ClockAngle(Local(set.Value, zone).TimeOfDay);
        var sweep = NormalizeDegrees(end - start);
        if (sweep == 0 && span.TotalHours >= 23)
            sweep = 360;
        return new DaylightCycle(start, sweep, Math.Clamp((now - rise.Value) / span, 0, 1),
            day.DaylightSeconds ?? span.TotalSeconds, rise, set);
    }
}
