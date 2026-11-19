using TimeZoneConverter;

namespace AmbientWeatherDashboard.Core.Models;

public static class WeatherMath
{
    public static double Temperature(double celsius, bool fahrenheit) => fahrenheit ? celsius * 9 / 5 + 32 : celsius;
    public static double WindSpeed(double kilometersPerHour, bool mph) => mph ? kilometersPerHour / 1.609344 : kilometersPerHour;
    public static double NormalizeDegrees(double degrees) => (degrees % 360 + 360) % 360;
    public static double ShortestTurn(double from, double to) => NormalizeDegrees(to - from + 180) - 180;
    public static double HumiditySweep(double percent) => Math.Clamp(percent, 0, 100) * 3.6;
    public static int UvLevel(double value) => value switch { < 3 => 0, < 6 => 1, < 8 => 2, < 11 => 3, _ => 4 };
    public static double LightIndex(double radiation) => Math.Clamp(radiation / 10, 0, 100);

    public static TimeZoneInfo? FindZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return null;
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var zone))
            return zone;

        return TZConvert.TryGetTimeZoneInfo(timeZone, out zone) ? zone : null;
    }

    public static DateTimeOffset Local(DateTimeOffset time, TimeZoneInfo zone) => TimeZoneInfo.ConvertTime(time, zone);

    public static double ClockAngle(TimeSpan wallTime) => NormalizeDegrees(wallTime.TotalHours * 15 - 180);

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

    private static bool Contains(DateTimeOffset now, DaylightCycle cycle) =>
        cycle.Sunrise is { } rise && cycle.Sunset is { } set && now >= rise && now < set;

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
