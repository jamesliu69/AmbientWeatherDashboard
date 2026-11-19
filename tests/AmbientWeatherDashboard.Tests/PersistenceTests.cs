using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;

namespace AmbientWeatherDashboard.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task SettingsRoundTripAndCorruptSettingsFallBackToDefaults()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(temp.Path);
        Assert.Equal(AppSettings.Default, await service.LoadAsync(default));
        var location = new LocationInfo("Tokyo", null, "Japan", 35.68, 139.69, "Asia/Tokyo");
        var settings = new AppSettings(location, true, true, true, true);

        await service.SaveAsync(settings, default);
        Assert.Equal(settings, await service.LoadAsync(default));
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "settings.json"), "{invalid");
        Assert.Equal(AppSettings.Default, await service.LoadAsync(default));
    }

    [Fact]
    public async Task WeatherCacheKeepsLastValidResponsePerLocationAndHandlesCorruption()
    {
        using var temp = new TempDirectory();
        var cache = new WeatherCacheService(temp.Path);
        var location = LocationInfo.Taipei;
        var source = ForecastFixture.Element();
        var now = ForecastFixture.Now;
        var snapshot = OpenMeteoParser.Parse(source, now);

        Assert.Null(await cache.LoadAsync(location, default));
        await cache.SaveAsync(location, new WeatherPayload(now, source, snapshot), default);
        var loaded = Assert.IsType<WeatherSnapshot>(await cache.LoadAsync(location, default));
        Assert.Equal(snapshot.FetchedAt, loaded.FetchedAt);
        Assert.Equal(snapshot.Current, loaded.Current);
        Assert.Null(await cache.LoadAsync(location with { Longitude = 130 }, default));

        var file = Assert.Single(Directory.GetFiles(Path.Combine(temp.Path, "weather-cache")));
        await File.WriteAllTextAsync(file, "[");
        Assert.Null(await cache.LoadAsync(location, default));
    }

    [Fact]
    public async Task CanceledSettingsWriteKeepsPreviousFile()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(temp.Path);
        await service.SaveAsync(AppSettings.Default, default);
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.SaveAsync(AppSettings.Default with { Fahrenheit = true }, canceled.Token));
        Assert.Equal(AppSettings.Default, await service.LoadAsync(default));
        Assert.Single(Directory.GetFiles(temp.Path));
    }
}
