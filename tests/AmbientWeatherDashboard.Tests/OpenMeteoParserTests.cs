using System.Text.Json;
using AmbientWeatherDashboard.Core.Services;

namespace AmbientWeatherDashboard.Tests;

public sealed class OpenMeteoParserTests
{
    [Fact]
    public void ParsesCurrentHourlyAndDailySeriesWithCorrectUnitsAndIndexAlignment()
    {
        var snapshot = OpenMeteoParser.Parse(ForecastFixture.Element(), ForecastFixture.Now);

        Assert.Equal(TimeSpan.FromHours(8), snapshot.Zone.GetUtcOffset(snapshot.Current.Time));
        Assert.Equal(23, snapshot.Current.Temperature);
        Assert.Equal(70, snapshot.Current.Humidity);
        Assert.Equal(24, snapshot.Current.FeelsLike);
        Assert.Equal(0, snapshot.Current.WindDirection);
        Assert.Equal(19.3, snapshot.Current.WindSpeed);
        Assert.Equal(23.5, snapshot.Current.Gust);
        Assert.False(snapshot.Current.IsDay);

        Assert.Equal(4, snapshot.Hourly.Count);
        Assert.Equal(17, snapshot.Hourly[1].DewPoint);
        Assert.Equal(0.3, snapshot.Hourly[1].UvIndex);
        Assert.Equal(6, snapshot.Hourly[1].Radiation);
        Assert.Equal(41, snapshot.Hourly[2].WindSpeed);
        Assert.Equal(26, snapshot.Hourly[^1].Temperature);
        Assert.Null(snapshot.Hourly[^1].Code);

        Assert.Equal(new DateOnly(2026, 2, 5), snapshot.Daily[1].Date);
        Assert.Equal(ForecastFixture.Midnight.AddHours(7), snapshot.Daily[1].Sunrise);
        Assert.Equal(ForecastFixture.Midnight.AddHours(18), snapshot.Daily[1].Sunset);
        Assert.Equal(39600, snapshot.Daily[1].DaylightSeconds);
        Assert.Equal(27, snapshot.Daily[0].HighTemperature);
        Assert.Equal(4.5, snapshot.Daily[1].MaxUv);
    }

    [Fact]
    public void AcceptsRiseAndSetEventsOutsideTheCivilDateOfTheirDailyRow()
    {
        var boundary = ForecastFixture.Midnight;
        var source = ForecastFixture.Json().Replace(boundary.AddHours(7).ToUnixTimeSeconds().ToString(),
            boundary.AddHours(-4).ToUnixTimeSeconds().ToString());
        var snapshot = OpenMeteoParser.Parse(ForecastFixture.Element(source), ForecastFixture.Now);
        Assert.Equal(boundary.AddHours(-4), snapshot.Daily[1].Sunrise);
    }

    [Theory]
    [InlineData("Asia/Kathmandu", 20700, -45)]
    [InlineData("America/St_Johns", -12600, 30)]
    public void AlignsFractionalOffsetForecastSamplesWithoutChangingCurrentOrAlreadyAlignedTimes(
        string zone, int offset, int labelledMinutes)
    {
        var sample = ForecastFixture.Now.AddMinutes(-14);
        var labelled = sample.AddMinutes(labelledMinutes);
        var source = $$"""
            {"timezone":"{{zone}}","utc_offset_seconds":{{offset}},
             "current":{"time":{{sample.ToUnixTimeSeconds()}},"temperature_2m":23},
             "hourly":{"time":[{{labelled.ToUnixTimeSeconds()}}, {{sample.AddHours(1).ToUnixTimeSeconds()}}],
             "temperature_2m":[23,24]} }
            """;
        var snapshot = OpenMeteoParser.Parse(ForecastFixture.Element(source), ForecastFixture.Now);
        Assert.Equal(sample, snapshot.Current.Time);
        Assert.Equal(sample, snapshot.Hourly[0].Time);
        Assert.Equal(sample.AddHours(1), snapshot.Hourly[1].Time);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("-1")]
    [InlineData("101")]
    [InlineData("1e400")]
    [InlineData("\"NaN\"")]
    public void RejectsInvalidHumidityAndKeepsOtherMeasurements(string invalid)
    {
        var snapshot = OpenMeteoParser.Parse(ForecastFixture.Element(ForecastFixture.Json(invalid)), ForecastFixture.Now);

        Assert.Null(snapshot.Current.Humidity);
        Assert.Equal(23, snapshot.Current.Temperature);
        Assert.Null(snapshot.Hourly[2].Humidity);
    }

    [Fact]
    public void RecomputesDailyPeaksWhenTheApiOffsetWouldOmitPartOfTheCivilDay()
    {
        var fetched = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        var first = new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero);
        var hours = Enumerable.Range(0, 27).ToArray();
        var source = JsonSerializer.SerializeToElement(new
        {
            timezone = "America/New_York",
            utc_offset_seconds = -18000,
            current = new { time = fetched.ToUnixTimeSeconds(), temperature_2m = 10 },
            hourly = new
            {
                time = hours.Select(hour => first.AddHours(hour).ToUnixTimeSeconds()).ToArray(),
                temperature_2m = hours.Select(hour => hour == 1 ? 25 : 10).ToArray(),
                uv_index = hours.Select(hour => hour == 1 ? 6 : 1).ToArray()
            },
            daily = new
            {
                time = new[] { first.AddHours(2).ToUnixTimeSeconds() },
                temperature_2m_max = new[] { 10 },
                uv_index_max = new[] { 1 }
            }
        });
        var daily = Assert.Single(OpenMeteoParser.Parse(source, fetched).Daily);
        Assert.Equal(new DateOnly(2026, 11, 1), daily.Date);
        Assert.Equal(25, daily.HighTemperature);
        Assert.Equal(6, daily.MaxUv);
    }

    [Fact]
    public void MissingOptionalSeriesStayEmpty()
    {
        var now = ForecastFixture.Now;
        var source = $$"""
            {"timezone":"Asia/Taipei","utc_offset_seconds":28800,
             "current":{"time":{{now.ToUnixTimeSeconds()}},"temperature_2m":23} }
            """;
        var snapshot = OpenMeteoParser.Parse(ForecastFixture.Element(source), now);
        Assert.Empty(snapshot.Daily);
        Assert.Empty(snapshot.Hourly);
        Assert.Null(snapshot.Current.WindSpeed);
    }

    [Fact]
    public void InvalidTimestampOrUnrecognizablePayloadIsRejected()
    {
        var wrongTime = ForecastFixture.Json(currentTime: ForecastFixture.Now.AddDays(-1).ToUnixTimeSeconds());
        Assert.Throws<InvalidDataException>(() => OpenMeteoParser.Parse(ForecastFixture.Element(wrongTime), ForecastFixture.Now));
        Assert.Throws<InvalidDataException>(() => OpenMeteoParser.Parse(ForecastFixture.Element("[]"), ForecastFixture.Now));
    }
}
