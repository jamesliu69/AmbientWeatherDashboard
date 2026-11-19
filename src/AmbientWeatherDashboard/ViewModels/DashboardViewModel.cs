using System.Collections.ObjectModel;
using System.Windows;
using AmbientWeatherDashboard.Controls;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmbientWeatherDashboard.ViewModels;

public sealed partial class DashboardViewModel(WeatherRefreshService refresh, WeatherCacheService cache, TimeProvider clock)
    : ObservableObject
{
    private const string Missing = "—";
    private AppSettings settings = AppSettings.Default;
    private WeatherSnapshot? weather;
    private TimeZoneInfo zone = WeatherMath.FindZone(AppSettings.Default.Location.TimeZone)!;
    private CancellationTokenSource? lifetime;
    private Task? refreshLoop;
    private Task? pendingRefresh;
    private DateTimeOffset displayMinute;
    private bool minimized;
    private bool connected;
    private bool updating;
    private bool cacheWriteFailed;
    private string? failure;

    public ObservableCollection<ForecastTile> Forecast { get; } = [];
    public Visibility NoForecastVisibility => Forecast.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public SunMetricViewModel SolarMetric { get; } = new(1000, DialDrawing.Gold, "W/m²");
    public SunMetricViewModel UvMetric { get; } = new(11, DialDrawing.Violet, "");
    public SunMetricViewModel LightMetric { get; } = new(100, DialDrawing.Ice, "/ 100");

    public string WindUnit => settings.MilesPerHour ? "MPH" : "KM/H";
    public string TemperatureUnit => settings.Fahrenheit ? "°F" : "°C";
    public string TemperatureScale => Temperature == Missing ? "" : TemperatureUnit[1..];
    public string WindWithUnit => WindSpeed == Missing ? Missing : $"{WindSpeed} {WindUnit}";
    public string TemperatureAccessible => Temperature == Missing ? Text.NoData : $"{Temperature.TrimEnd('°')} {TemperatureUnit}";
    public string WindAccessible => WindDirection is { } angle ? $"{WindWithUnit} · {angle:0}°" : WindWithUnit;
    public string CurrentCondition => Text.Condition(CurrentCode, CurrentIsDay);
    public bool Motion => !settings.ReducedMotion && !minimized && SystemParameters.ClientAreaAnimation;
    public string City => settings.Location == LocationInfo.Taipei && settings.TraditionalChinese ? "臺北" : settings.Location.Name;
    public string StatusText => updating ? Text.Updating : connected ? Text.Connected : weather is null ? Text.NoData : Text.Offline;
    public string StatusDetail => (weather is null ? StatusText : LastUpdated) + (failure is null ? "" : $"\n{(cacheWriteFailed ? Text.CacheFailed : Text.ForecastUpdateFailed)}\n{failure}");
    public string LastUpdated => weather is { } snapshot
        ? $"{Text.LastUpdated} · {WeatherMath.Local(snapshot.FetchedAt, zone).ToString("yyyy-MM-dd HH:mm", Text.Culture)}"
        : "";

    [ObservableProperty] private UiText text = new(false);
    [ObservableProperty] private string localDate = "";
    [ObservableProperty] private string clockText = "--:--";
    [ObservableProperty] private double? windDirection;
    [ObservableProperty] private string windSpeed = Missing;
    [ObservableProperty] private string gust = Missing;
    [ObservableProperty] private double? humidity;
    [ObservableProperty] private string humidityText = Missing;
    [ObservableProperty] private string humidityDifference = Missing;
    [ObservableProperty] private string temperature = Missing;
    [ObservableProperty] private string dewPoint = Missing;
    [ObservableProperty] private string yesterdayHigh = Missing;
    [ObservableProperty] private string feelsLike = Missing;
    [ObservableProperty] private double sunStart;
    [ObservableProperty] private double sunSweep;
    [ObservableProperty] private double? sunProgress;
    [ObservableProperty] private string daylight = Missing;
    [ObservableProperty] private string sunrise = Missing;
    [ObservableProperty] private string sunset = Missing;
    [ObservableProperty] private ForecastTile? focusForecast;
    [ObservableProperty] private int? currentCode;
    [ObservableProperty] private bool? currentIsDay;

    public void Configure(AppSettings preferences)
    {
        var newLocation = settings.Location != preferences.Location;
        if (newLocation)
        {
            weather = null;
            connected = false;
            failure = null;
        }

        settings = preferences;
        zone = weather?.Zone ?? WeatherMath.FindZone(settings.Location.TimeZone)!;
        Text = new UiText(settings.TraditionalChinese);
        OnPropertyChanged(nameof(WindUnit));
        OnPropertyChanged(nameof(TemperatureUnit));
        OnPropertyChanged(nameof(City));
        OnPropertyChanged(nameof(Motion));
        Display();
        UpdateStatus();
    }

    public void SetMinimized(bool value)
    {
        minimized = value;
        OnPropertyChanged(nameof(Motion));
        if (!minimized)
            Display();
    }

    public void Start()
    {
        if (lifetime is not null)
            return;

        lifetime = new CancellationTokenSource();
        refreshLoop = RunAsync(lifetime.Token);
        RefreshCommand.NotifyCanExecuteChanged();
    }

    public async Task StopAsync()
    {
        if (lifetime is not { } active)
            return;

        await active.CancelAsync();
        try
        {
            await Task.WhenAll(refreshLoop ?? Task.CompletedTask, pendingRefresh ?? Task.CompletedTask);
        }
        catch (OperationCanceledException) when (active.IsCancellationRequested)
        {
            // Navigation canceled the current refresh.
        }
        finally
        {
            active.Dispose();
            lifetime = null;
            refreshLoop = null;
            pendingRefresh = null;
            updating = false;
            UpdateStatus();
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (weather is null)
        {
            updating = true;
            UpdateStatus();
            var location = settings.Location;
            var saved = await Task.Run(() => cache.LoadAsync(location, cancellationToken), cancellationToken);
            if (saved is not null && !cancellationToken.IsCancellationRequested && weather is null)
                DisplaySnapshot(saved);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15), clock);
        await GetRefresh(cancellationToken);
        while (await timer.WaitForNextTickAsync(cancellationToken))
            await GetRefresh(cancellationToken);
    }

    private bool CanRefresh() => lifetime is { IsCancellationRequested: false };

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (lifetime is not { IsCancellationRequested: false } active)
            return;
        var token = active.Token;
        try
        {
            await GetRefresh(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The current dashboard was closed.
        }
    }

    private Task GetRefresh(CancellationToken token)
    {
        if (pendingRefresh is null || pendingRefresh.IsCompleted)
            pendingRefresh = RefreshOnceAsync(token);
        return pendingRefresh;
    }

    private async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        updating = true;
        UpdateStatus();
        try
        {
            var result = await refresh.RefreshAsync(settings.Location, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            connected = result.Connected;
            failure = result.Error;
            cacheWriteFailed = result.CacheWriteFailed;
            if (result.Snapshot is { } snapshot && (weather is null || snapshot.FetchedAt >= weather.FetchedAt))
                DisplaySnapshot(snapshot);
        }
        finally
        {
            updating = false;
            UpdateStatus();
        }
    }

    private void DisplaySnapshot(WeatherSnapshot snapshot)
    {
        weather = snapshot;
        zone = snapshot.Zone;
        Display();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusDetail));
        OnPropertyChanged(nameof(LastUpdated));
    }

    public void Tick()
    {
        var now = clock.GetUtcNow();
        if ((now - displayMinute).Duration() < TimeSpan.FromMinutes(1) && now >= displayMinute)
            return;

        if (!minimized && lifetime is not null)
            Display();
        else
            DisplayClock(now);
    }

    private void DisplayClock(DateTimeOffset now)
    {
        displayMinute = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMinute));
        var local = WeatherMath.Local(now, zone);
        ClockText = local.ToString("HH:mm", Text.Culture);
        LocalDate = local.ToString(settings.TraditionalChinese ? "yyyy年M月d日 ddd" : "ddd, MMM d, yyyy", Text.Culture);
    }

    private void Display()
    {
        var now = clock.GetUtcNow();
        DisplayClock(now);
        var local = WeatherMath.Local(now, zone);
        var date = DateOnly.FromDateTime(local.DateTime);
        var today = weather?.Daily.FirstOrDefault(day => day.Date == date);
        var yesterday = weather?.Daily.FirstOrDefault(day => day.Date == date.AddDays(-1));
        var tomorrow = weather?.Daily.FirstOrDefault(day => day.Date == date.AddDays(1));
        var current = weather?.Current;
        var latestHour = current is null ? null : weather!.Hourly.LastOrDefault(hour => hour.Time <= current.Time
            && current.Time - hour.Time < TimeSpan.FromHours(1));
        var sameHourYesterday = latestHour is null ? null : WeatherMath.SameHourYesterday(weather!.Hourly, latestHour.Time, zone);
        var cycle = WeatherMath.ActiveSunCycle(now, today, yesterday, tomorrow, zone);

        CurrentCode = current?.Code;
        CurrentIsDay = cycle.Seconds switch
        {
            0 => false,
            86400 => true,
            _ when cycle.Sunrise is { } rise && cycle.Sunset is { } set => now >= rise && now < set,
            _ => current?.IsDay
        };
        WindDirection = current?.WindSpeed is 0 ? null : current?.WindDirection;
        WindSpeed = FormatWind(current?.WindSpeed);
        Gust = current?.Gust is { } gustValue ? $"{FormatWind(gustValue)} {WindUnit}" : Missing;
        Humidity = current?.Humidity;
        HumidityText = Humidity is { } humidityValue ? $"{Format(humidityValue, "0")}%" : Missing;
        HumidityDifference = latestHour?.Humidity is { } todayHumidity && sameHourYesterday?.Humidity is { } previous
            ? Text.PercentagePoints(todayHumidity - previous)
            : Missing;
        Temperature = current?.Temperature is { } temp ? $"{Format(WeatherMath.Temperature(temp, settings.Fahrenheit), "0")}°" : Missing;
        DewPoint = FormatTemperature(latestHour?.DewPoint);
        YesterdayHigh = FormatTemperature(yesterday?.HighTemperature);
        FeelsLike = FormatTemperature(current?.FeelsLike);

        SunStart = cycle.StartAngle;
        SunSweep = cycle.Sweep;
        SunProgress = cycle.Progress;
        Daylight = cycle.Seconds is { } seconds ? Text.DaylightDuration(seconds) : Missing;
        Sunrise = FormatSunTime(cycle.Sunrise, date);
        Sunset = FormatSunTime(cycle.Sunset, date);

        DisplaySun(date, today, latestHour);
        DisplayForecast(now);

        OnPropertyChanged(nameof(WindWithUnit));
        OnPropertyChanged(nameof(WindAccessible));
        OnPropertyChanged(nameof(TemperatureAccessible));
        OnPropertyChanged(nameof(TemperatureScale));
        OnPropertyChanged(nameof(CurrentCondition));
    }

    private void DisplaySun(DateOnly today, DailyForecast? daily, HourlyForecast? hour)
    {
        var peak = weather?.Hourly
            .Where(sample => DateOnly.FromDateTime(WeatherMath.Local(sample.Time.AddTicks(-1), zone).DateTime) == today)
            .Max(sample => sample.Radiation);
        SolarMetric.Display(Text.Solar, hour?.Radiation, Format(hour?.Radiation, "0"),
            $"{Text.Peak} · {Format(peak, "0")}{(peak.HasValue ? " W/m²" : "")}");
        var uvCategory = hour?.UvIndex is { } uv ? $"{Text.UvLevel(WeatherMath.UvLevel(uv))}\n" : "";
        UvMetric.Display(Text.Uv, hour?.UvIndex, Format(hour?.UvIndex, "0.0"),
            $"{uvCategory}{Text.Peak} · {Format(daily?.MaxUv, "0.0")}");
        double? light = hour?.Radiation is { } radiation ? WeatherMath.LightIndex(radiation) : null;
        LightMetric.Display(Text.Light, light, Format(light, "0.0"), Text.Relative);
    }

    private void DisplayForecast(DateTimeOffset now)
    {
        var tiles = (weather?.Hourly ?? []).Where(hour => hour.Time > now).Take(5).Select(hour =>
        {
            var local = WeatherMath.Local(hour.Time, zone);
            var windy = hour.WindSpeed is >= 40;
            var caption = $"{local:yyyy-MM-dd HH:mm zzz} · {Text.Condition(hour.Code, hour.IsDay)}";
            if (windy && hour.WindSpeed is { } speed)
                caption += $" · {Text.Windy} ({FormatWind(speed)} {WindUnit})";

            return new ForecastTile(hour.Time, local.ToString("HH:mm", Text.Culture), hour.Code, hour.IsDay, windy,
                FormatTemperature(hour.Temperature), caption);
        }).ToArray();

        while (Forecast.Count > 0 && (tiles.Length == 0 || Forecast[0].Timestamp < tiles[0].Timestamp))
            Forecast.RemoveAt(0);
        for (var index = 0; index < tiles.Length; index++)
        {
            if (index == Forecast.Count)
                Forecast.Add(tiles[index]);
            else if (Forecast[index] != tiles[index])
                Forecast[index] = tiles[index];
        }
        while (Forecast.Count > tiles.Length)
            Forecast.RemoveAt(Forecast.Count - 1);

        FocusForecast = tiles.FirstOrDefault(tile => tile.Code.HasValue);
        OnPropertyChanged(nameof(NoForecastVisibility));
    }

    private string FormatSunTime(DateTimeOffset? time, DateOnly today)
    {
        if (time is null)
            return Missing;
        var local = WeatherMath.Local(time.Value, zone);
        var format = DateOnly.FromDateTime(local.DateTime) == today ? "HH:mm" : "MM/dd HH:mm";
        return local.ToString(format, Text.Culture);
    }

    private string Format(double? value, string pattern) => value?.ToString(pattern, Text.Culture) ?? Missing;

    private string FormatTemperature(double? value) => value is { } temp
        ? $"{Format(WeatherMath.Temperature(temp, settings.Fahrenheit), "0")}{TemperatureUnit}"
        : Missing;

    private string FormatWind(double? value) => value is { } wind
        ? Format(WeatherMath.WindSpeed(wind, settings.MilesPerHour), "0.0")
        : Missing;
}
