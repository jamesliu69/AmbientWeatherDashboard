using System.Collections.ObjectModel;
using System.Windows;
using AmbientWeatherDashboard.Controls;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmbientWeatherDashboard.ViewModels;

/// <summary>管理目前天氣、逐時預報、日照指標及儀表板顯示狀態。</summary>
public sealed partial class DashboardViewModel(WeatherRefreshService refresh, WeatherCacheService cache, TimeProvider clock)
    : ObservableObject
{
    /// <summary>在畫面沒有可用測量值時顯示的佔位文字。</summary>
    private const string Missing = "—";
    /// <summary>目前套用至儀表板的使用者偏好。</summary>
    private AppSettings settings = AppSettings.Default;
    /// <summary>目前顯示的天氣快照。</summary>
    private WeatherSnapshot? weather;
    /// <summary>目前地點的時區，用於日期、時間與日照計算。</summary>
    private TimeZoneInfo zone = WeatherMath.FindZone(AppSettings.Default.Location.TimeZone)!;
    /// <summary>控制週期更新和手動更新生命週期的取消來源。</summary>
    private CancellationTokenSource? lifetime;
    /// <summary>執行初次與定期天氣更新的背景工作。</summary>
    private Task? refreshLoop;
    /// <summary>供週期更新與手動命令共用的單次更新工作。</summary>
    private Task? pendingRefresh;
    /// <summary>記錄時鐘最近一次顯示的分鐘邊界，避免同一分鐘重複整理畫面。</summary>
    private DateTimeOffset displayMinute;
    /// <summary>記錄儀表板目前是否最小化。</summary>
    private bool minimized;
    /// <summary>記錄最近一次更新結果的連線狀態。</summary>
    private bool connected;
    /// <summary>記錄目前是否有天氣更新工作進行中。</summary>
    private bool updating;
    /// <summary>記錄最近一次天氣快取寫入是否失敗。</summary>
    private bool cacheWriteFailed;
    /// <summary>保存最近一次更新錯誤訊息。</summary>
    private string? failure;

    /// <summary>保存畫面目前顯示的逐時預報項目。</summary>
    public ObservableCollection<ForecastTile> Forecast { get; } = [];
    public Visibility NoForecastVisibility => Forecast.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>顯示目前輻射值與今日峰值的太陽輻射指標。</summary>
    public SunMetricViewModel SolarMetric { get; } = new(1000, DialDrawing.Gold, "W/m²");
    /// <summary>顯示目前紫外線指數與級別的指標。</summary>
    public SunMetricViewModel UvMetric { get; } = new(11, DialDrawing.Violet, "");
    /// <summary>以 0 至 100 相對尺度顯示估計光照的指標。</summary>
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

    /// <summary>保存介面目前語言對應的文字資源。</summary>
    [ObservableProperty] private UiText text = new(false);
    /// <summary>保存地點時區中的本地日期文字。</summary>
    [ObservableProperty] private string localDate = "";
    /// <summary>保存地點時區中的時鐘文字。</summary>
    [ObservableProperty] private string clockText = "--:--";
    /// <summary>保存目前風向角度；無風或資料缺少時為 null。</summary>
    [ObservableProperty] private double? windDirection;
    /// <summary>保存依設定單位格式化的目前風速。</summary>
    [ObservableProperty] private string windSpeed = Missing;
    /// <summary>保存依設定單位格式化的目前陣風速度。</summary>
    [ObservableProperty] private string gust = Missing;
    /// <summary>保存目前相對濕度百分比。</summary>
    [ObservableProperty] private double? humidity;
    /// <summary>保存目前相對濕度的顯示文字。</summary>
    [ObservableProperty] private string humidityText = Missing;
    /// <summary>保存目前濕度與昨日同一當地小時濕度的百分點差。</summary>
    [ObservableProperty] private string humidityDifference = Missing;
    /// <summary>保存依設定單位格式化的目前氣溫。</summary>
    [ObservableProperty] private string temperature = Missing;
    /// <summary>保存最近逐時預報的露點溫度顯示文字。</summary>
    [ObservableProperty] private string dewPoint = Missing;
    /// <summary>保存昨日最高溫的顯示文字。</summary>
    [ObservableProperty] private string yesterdayHigh = Missing;
    /// <summary>保存目前體感溫度的顯示文字。</summary>
    [ObservableProperty] private string feelsLike = Missing;
    /// <summary>保存日照弧線起始角度。</summary>
    [ObservableProperty] private double sunStart;
    /// <summary>保存日照弧線涵蓋的角度。</summary>
    [ObservableProperty] private double sunSweep;
    /// <summary>保存日照週期已經過的比例，資料不足時為 null。</summary>
    [ObservableProperty] private double? sunProgress;
    /// <summary>保存日照時長的本地化顯示文字。</summary>
    [ObservableProperty] private string daylight = Missing;
    /// <summary>保存日出時間的本地化顯示文字。</summary>
    [ObservableProperty] private string sunrise = Missing;
    /// <summary>保存日落時間的本地化顯示文字。</summary>
    [ObservableProperty] private string sunset = Missing;
    /// <summary>保存目前焦點預報項目；尚無含天氣代碼的項目時為 null。</summary>
    [ObservableProperty] private ForecastTile? focusForecast;
    /// <summary>保存目前天氣代碼，供狀況文字與圖示使用。</summary>
    [ObservableProperty] private int? currentCode;
    /// <summary>保存目前時段是否為白天的資料。</summary>
    [ObservableProperty] private bool? currentIsDay;

    /// <summary>套用位置、單位與語言偏好；位置改變時清除舊天氣資料。</summary>
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

    /// <summary>更新最小化狀態，並在還原時重新整理儀表板內容。</summary>
    public void SetMinimized(bool value)
    {
        minimized = value;
        OnPropertyChanged(nameof(Motion));
        if (!minimized)
            Display();
    }

    /// <summary>建立更新取消來源並啟動初次及定期天氣更新；重複呼叫不會另建工作。</summary>
    public void Start()
    {
        if (lifetime is not null)
            return;

        lifetime = new CancellationTokenSource();
        refreshLoop = RunAsync(lifetime.Token);
        RefreshCommand.NotifyCanExecuteChanged();
    }

    /// <summary>取消更新工作並等待它們結束，然後清除生命週期狀態。</summary>
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
            // 切換頁面或關閉應用程式時，取消目前的天氣更新工作。
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

    /// <summary>沒有快照時先載入快取，之後立即更新並以 15 分鐘週期重複更新。</summary>
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

    /// <summary>僅在儀表板更新生命週期仍有效時允許執行手動更新命令。</summary>
    private bool CanRefresh() => lifetime is { IsCancellationRequested: false };

    /// <summary>使用目前生命週期的取消權杖執行或共用更新工作。</summary>
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
            // 儀表板工作階段停止時，忽略更新命令的預期取消結果。
        }
    }

    /// <summary>重用尚未完成的更新工作，否則建立新的單次更新工作。</summary>
    private Task GetRefresh(CancellationToken token)
    {
        if (pendingRefresh is null || pendingRefresh.IsCompleted)
            pendingRefresh = RefreshOnceAsync(token);
        return pendingRefresh;
    }

    /// <summary>更新連線與錯誤狀態，並只接受時間不早於目前快照的新快照。</summary>
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

    /// <summary>替換目前快照與時區，並重新整理畫面及狀態文字。</summary>
    private void DisplaySnapshot(WeatherSnapshot snapshot)
    {
        weather = snapshot;
        zone = snapshot.Zone;
        Display();
        UpdateStatus();
    }

    /// <summary>通知繫結更新連線狀態、狀態細節與最後更新時間。</summary>
    private void UpdateStatus()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusDetail));
        OnPropertyChanged(nameof(LastUpdated));
    }

    /// <summary>每分鐘檢查時鐘；活動中的儀表板更新全部顯示，其他狀態只更新日期與時間。</summary>
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

    /// <summary>將目前時間轉為地點時區，更新分鐘邊界、時鐘及本地日期。</summary>
    private void DisplayClock(DateTimeOffset now)
    {
        displayMinute = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMinute));
        var local = WeatherMath.Local(now, zone);
        ClockText = local.ToString("HH:mm", Text.Culture);
        LocalDate = local.ToString(settings.TraditionalChinese ? "yyyy年M月d日 ddd" : "ddd, MMM d, yyyy", Text.Culture);
    }

    /// <summary>從目前快照整理天氣、日照、濕度比較及預報顯示屬性。</summary>
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

    /// <summary>更新目前太陽輻射、紫外線與估計光照指標及今日輻射峰值。</summary>
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

    /// <summary>以本地時間建立接下來五筆逐時預報，更新集合並選出第一筆有天氣代碼的項目。</summary>
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

    /// <summary>以地點時區格式化日出或日落時間，跨日期時附上月日。</summary>
    private string FormatSunTime(DateTimeOffset? time, DateOnly today)
    {
        if (time is null)
            return Missing;
        var local = WeatherMath.Local(time.Value, zone);
        var format = DateOnly.FromDateTime(local.DateTime) == today ? "HH:mm" : "MM/dd HH:mm";
        return local.ToString(format, Text.Culture);
    }

    /// <summary>依目前介面文化格式化數值，值缺少時回傳佔位文字。</summary>
    private string Format(double? value, string pattern) => value?.ToString(pattern, Text.Culture) ?? Missing;

    /// <summary>依溫度單位偏好格式化數值，缺少時回傳佔位文字。</summary>
    private string FormatTemperature(double? value) => value is { } temp
        ? $"{Format(WeatherMath.Temperature(temp, settings.Fahrenheit), "0")}{TemperatureUnit}"
        : Missing;

    /// <summary>依風速單位偏好換算並格式化數值，缺少時回傳佔位文字。</summary>
    private string FormatWind(double? value) => value is { } wind
        ? Format(WeatherMath.WindSpeed(wind, settings.MilesPerHour), "0.0")
        : Missing;
}
