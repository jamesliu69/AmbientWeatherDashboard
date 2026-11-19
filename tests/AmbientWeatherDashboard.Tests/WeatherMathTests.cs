using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Tests;

public sealed class WeatherMathTests
{
    [Fact]
    public void ConvertsTemperatureWindSpeedAndEstimatedLight()
    {
        Assert.Equal(32, WeatherMath.Temperature(0, true));
        Assert.Equal(-40, WeatherMath.Temperature(-40, true));
        Assert.Equal(23, WeatherMath.Temperature(23, false));
        Assert.Equal(10, WeatherMath.WindSpeed(16.09344, true), 10);
        Assert.Equal(16.09344, WeatherMath.WindSpeed(16.09344, false));
        Assert.Equal(35, WeatherMath.LightIndex(350));
        Assert.Equal(100, WeatherMath.LightIndex(1300));
    }

    [Theory]
    [InlineData(350, 10, 20)]
    [InlineData(10, 350, -20)]
    [InlineData(90, 90, 0)]
    public void CompassTurnsAlongTheShortestArc(double from, double to, double delta)
    {
        Assert.Equal(delta, WeatherMath.ShortestTurn(from, to));
        Assert.Equal(to, WeatherMath.NormalizeDegrees(from + delta));
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(0, 0)]
    [InlineData(70, 252)]
    [InlineData(100, 360)]
    [InlineData(101, 360)]
    public void HumidityArcSupportsFullRange(double value, double sweep) => Assert.Equal(sweep, WeatherMath.HumiditySweep(value));

    [Theory]
    [InlineData(2.9, 0)]
    [InlineData(3, 1)]
    [InlineData(6, 2)]
    [InlineData(8, 3)]
    [InlineData(11, 4)]
    public void ClassifiesUv(double value, int level) => Assert.Equal(level, WeatherMath.UvLevel(value));

    [Fact]
    public void DayArcAndElapsedProgressUseLocalTimeAndApiDaylightDuration()
    {
        var date = new DateOnly(2026, 2, 5);
        var rise = At(date, 6);
        var set = At(date, 18);
        var day = new DailyForecast(date, rise, set, 43205, null, null);
        var midday = WeatherMath.SunCycle(At(date, 12), day, TimeZoneInfo.Utc);
        Assert.Equal(270, midday.StartAngle);
        Assert.Equal(180, midday.Sweep);
        Assert.Equal(0.5, midday.Progress);
        Assert.Equal(43205, midday.Seconds);
        Assert.Equal(rise, midday.Sunrise);
        Assert.Equal(set, midday.Sunset);
        Assert.Equal(0, WeatherMath.SunCycle(rise.AddHours(-1), day, TimeZoneInfo.Utc).Progress);
        Assert.Equal(1, WeatherMath.SunCycle(set.AddHours(1), day, TimeZoneInfo.Utc).Progress);
        Assert.Equal(43200, WeatherMath.SunCycle(rise, day with { DaylightSeconds = null }, TimeZoneInfo.Utc).Seconds);
    }

    [Fact]
    public void SupportsCrossMidnightAndPolarDays()
    {
        var date = new DateOnly(2026, 6, 15);
        var day = new DailyForecast(date, At(date, 22), At(date, 2), null, null, null);
        var midnight = At(date.AddDays(1), 0);
        var wrapped = WeatherMath.SunCycle(midnight, day, TimeZoneInfo.Utc);
        Assert.Equal(150, wrapped.StartAngle);
        Assert.Equal(60, wrapped.Sweep);
        Assert.Equal(0.5, wrapped.Progress);
        Assert.Equal(14400, wrapped.Seconds);
        Assert.Equal(midnight.AddHours(2), wrapped.Sunset);
        var newDate = date.AddDays(1);
        var next = new DailyForecast(newDate, At(newDate, 22), At(newDate.AddDays(1), 2), null, null, null);
        Assert.Equal(wrapped, WeatherMath.ActiveSunCycle(midnight, next, day, null, TimeZoneInfo.Utc));
        Assert.Equal(next.Sunrise, WeatherMath.ActiveSunCycle(midnight.AddHours(3), next, day, null, TimeZoneInfo.Utc).Sunrise);

        var polarDay = WeatherMath.SunCycle(At(date, 12), day with { DaylightSeconds = 86400 }, TimeZoneInfo.Utc);
        Assert.Equal(360, polarDay.Sweep);
        Assert.Equal(0.5, polarDay.Progress);
        Assert.Null(polarDay.Sunrise);
        var polarNight = WeatherMath.SunCycle(midnight, day with { DaylightSeconds = 0 }, TimeZoneInfo.Utc);
        Assert.Equal(0, polarNight.Sweep);
        Assert.Equal(0, polarNight.Progress);
        Assert.Null(polarNight.Sunset);
    }

    [Fact]
    public void UsesTomorrowRowWhenTheSunRisesBeforeLocalMidnight()
    {
        var date = new DateOnly(2026, 6, 15);
        var today = new DailyForecast(date, At(date.AddDays(-1), 22), At(date, 20), null, null, null);
        var tomorrow = new DailyForecast(date.AddDays(1), At(date, 22), At(date.AddDays(1), 20), null, null, null);
        var cycle = WeatherMath.ActiveSunCycle(At(date, 23), today, null, tomorrow, TimeZoneInfo.Utc);
        Assert.Equal(tomorrow.Sunrise, cycle.Sunrise);
        Assert.Equal(1d / 22, cycle.Progress!.Value, 10);
    }

    [Fact]
    public void SameLocalHourUsesPreviousCalendarDateAndHandlesDst()
    {
        var zone = WeatherMath.FindZone("America/New_York")!;
        var date = new DateOnly(2026, 3, 8);
        var now = At(date, 7);
        var yesterdaySameHour = At(date.AddDays(-1), 8);
        var wrongHour = At(date.AddDays(-1), 7);
        var series = new[] { Sample(wrongHour), Sample(yesterdaySameHour) };
        Assert.Equal(3, WeatherMath.Local(now, zone).Hour);
        Assert.Equal(yesterdaySameHour, WeatherMath.SameHourYesterday(series, now, zone)?.Time);

        var taipei = WeatherMath.FindZone("Asia/Taipei")!;
        var beforeMidnightUtc = At(date, 19);
        Assert.Equal(date.AddDays(1), DateOnly.FromDateTime(WeatherMath.Local(beforeMidnightUtc, taipei).DateTime));
    }

    [Fact]
    public void AmbiguousPreviousDayHourPrefersMatchingUtcOffset()
    {
        var zone = WeatherMath.FindZone("America/New_York")!;
        var fallBack = new DateOnly(2026, 11, 1);
        var early = At(fallBack, 5);
        var late = At(fallBack, 6);
        var tomorrow = At(fallBack.AddDays(1), 6);
        Assert.Equal(late, WeatherMath.SameHourYesterday(new[] { Sample(early), Sample(late) }, tomorrow, zone)?.Time);
    }

    private static DateTimeOffset At(DateOnly date, int hour) =>
        new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private static HourlyForecast Sample(DateTimeOffset time) => new(time, null, 70, null, null, null, null, null);
}
