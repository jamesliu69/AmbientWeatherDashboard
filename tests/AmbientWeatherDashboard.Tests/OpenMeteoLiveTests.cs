using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;

namespace AmbientWeatherDashboard.Tests;

public sealed class OpenMeteoLiveTests
{
    [Fact]
    [Trait("Category", "Live")]
    public async Task SearchesTaipeiAndLoadsRealForecastThroughTheRefreshService()
    {
        using var time = new CancellationTokenSource(TimeSpan.FromSeconds(35));
        using var temp = new TempDirectory();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var geocoding = new GeocodingService(client);
        var result = await geocoding.SearchAsync("Taipei", false, time.Token);
        Assert.Contains(result, location => location.TimeZone == "Asia/Taipei");

        var location = LocationInfo.Taipei;
        var cache = new WeatherCacheService(temp.Path);
        var weather = new OpenMeteoService(client, TimeProvider.System);
        var refresh = new WeatherRefreshService(weather, cache);
        var fetched = await refresh.RefreshAsync(location, time.Token);
        Assert.True(fetched.Connected, fetched.Error);
        Assert.False(fetched.CacheWriteFailed, fetched.Error);
        var forecast = Assert.IsType<WeatherSnapshot>(fetched.Snapshot);
        Assert.Equal(TimeSpan.FromHours(8), forecast.Zone.GetUtcOffset(forecast.Current.Time));
        Assert.NotNull(forecast.Current.Temperature);
        Assert.NotNull(forecast.Current.Humidity);
        Assert.NotNull(forecast.Current.WindDirection);
        Assert.NotNull(forecast.Current.Gust);
        Assert.Contains(forecast.Hourly, hour => hour.DewPoint.HasValue && hour.UvIndex.HasValue && hour.Radiation.HasValue
            && hour.WindSpeed.HasValue);
        Assert.Contains(forecast.Daily, day => day.Sunrise.HasValue && day.Sunset.HasValue && day.DaylightSeconds.HasValue);
        Assert.Equal(forecast.Current, (await cache.LoadAsync(location, time.Token))?.Current);
    }
}
