using System.IO;
using System.Diagnostics;
using System.Windows;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmbientWeatherDashboard.ViewModels;

/// <summary>協調應用程式初始化、頁面切換、位置選擇與設定儲存。</summary>
public sealed partial class MainViewModel(SettingsService settingsService, DashboardViewModel dashboard,
    HomeViewModel home, SettingsViewModel settings) : ObservableObject
{
    /// <summary>控制應用程式初始化、頁面切換與關閉流程的取消來源。</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>目前套用的應用程式偏好。</summary>
    private AppSettings preferences = AppSettings.Default;
    /// <summary>標記頁面切換期間是否停用導覽與設定命令。</summary>
    private bool transitioning = true;
    /// <summary>標記應用程式關閉流程已開始。</summary>
    private bool closing;
    /// <summary>標記設定載入及初始儀表板啟動是否已完成。</summary>
    private bool initialized;
    /// <summary>保存共用的初始化工作，避免重複載入設定。</summary>
    private Task? initialize;

    public DashboardViewModel Dashboard { get; } = dashboard;
    public HomeViewModel Home { get; } = home;
    public SettingsViewModel Settings { get; } = settings;
    public UiText Text => Dashboard.Text;
    public Visibility DashboardVisibility => initialized && CurrentPage == Page.Dashboard ? Visibility.Visible : Visibility.Collapsed;
    public Visibility HomeVisibility => CurrentPage == Page.Home ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SettingsShortcutVisibility => CurrentPage == Page.Settings ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SettingsVisibility => CurrentPage == Page.Settings ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>保存目前選取的主要頁面。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DashboardVisibility), nameof(HomeVisibility), nameof(SettingsVisibility), nameof(SettingsShortcutVisibility))]
    private Page currentPage = Page.Dashboard;

    /// <summary>保存設定儲存失敗時供介面顯示的錯誤標題。</summary>
    [ObservableProperty] private string? error;
    /// <summary>保存設定儲存例外的詳細訊息。</summary>
    [ObservableProperty] private string? errorDetail;

    /// <summary>啟動並重用唯一的應用程式初始化工作。</summary>
    public Task StartAsync() => initialize ??= InitializeAsync();

    /// <summary>載入設定、設定各頁面 ViewModel，完成初始化後啟動儀表板。</summary>
    private async Task InitializeAsync()
    {
        try
        {
            preferences = await Task.Run(() => settingsService.LoadAsync(lifetime.Token), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();
            ApplyPreferences();
            initialized = true;
            OnPropertyChanged(nameof(DashboardVisibility));
            Dashboard.Start();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            // The application was closed during startup.
            // 應用程式在初始化完成前關閉時，略過取消工作的結果。
        }
        finally
        {
            SetTransitioning(false);
        }
    }

    /// <summary>將目前偏好套用至儀表板、首頁與設定頁，並通知介面文字更新。</summary>
    private void ApplyPreferences()
    {
        Dashboard.Configure(preferences);
        Home.Configure(preferences);
        Settings.Configure(preferences);
        OnPropertyChanged(nameof(Text));
    }

    /// <summary>只在未切換、未關閉且目標頁面不同時允許導覽。</summary>
    private bool CanNavigate(Page page) => !transitioning && !closing && CurrentPage != page;

    /// <summary>要求切換至指定頁面。</summary>
    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private Task NavigateAsync(Page page) => TransitionAsync(page, null);

    /// <summary>只允許選取有效地點，且目前不在切換或關閉流程中。</summary>
    private bool CanSelectLocation(LocationInfo? location) => !transitioning && !closing && location is { IsValid: true };

    /// <summary>將有效地點設為偏好位置並切換至儀表板；未選取地點時不執行動作。</summary>
    [RelayCommand(CanExecute = nameof(CanSelectLocation))]
    private Task SelectLocationAsync(LocationInfo? location) => location is null
        ? Task.CompletedTask
        : TransitionAsync(Page.Dashboard, preferences with { Location = location });

    /// <summary>只在頁面未切換且應用程式未關閉時允許儲存設定。</summary>
    private bool CanSaveSettings() => !transitioning && !closing;

    /// <summary>將設定頁的選項轉成偏好資料並要求儲存。</summary>
    [RelayCommand(CanExecute = nameof(CanSaveSettings))]
    private Task SaveSettingsAsync() => TransitionAsync(Page.Dashboard, Settings.ToSettings(preferences.Location));

    /// <summary>儲存可選的新偏好、停止目前頁面、套用設定並切換目標頁面。</summary>
    private async Task TransitionAsync(Page target, AppSettings? updates)
    {
        SetTransitioning(true);
        Error = null;
        ErrorDetail = null;
        try
        {
            if (updates is not null)
            {
                await Task.Run(() => settingsService.SaveAsync(updates, lifetime.Token), lifetime.Token);
                preferences = updates;
            }

            if (CurrentPage == Page.Dashboard)
                await Dashboard.StopAsync();
            if (CurrentPage == Page.Home)
                await Home.StopAsync();

            lifetime.Token.ThrowIfCancellationRequested();
            if (updates is not null)
                ApplyPreferences();
            if (target == Page.Settings)
                Settings.Configure(preferences);

            CurrentPage = target;
            if (target == Page.Dashboard)
                Dashboard.Start();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            // The application was closed during navigation.
            // 應用程式於頁面切換期間關閉時，略過取消工作的結果。
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("Could not save settings: {0}", ex.Message);
            // 檔案寫入或存取被拒時，保留錯誤訊息供介面顯示。
            Error = Text.SaveFailed;
            ErrorDetail = ex.Message;
        }
        finally
        {
            SetTransitioning(false);
        }
    }

    /// <summary>更新切換狀態並重新檢查頁面、位置與設定命令的執行條件。</summary>
    private void SetTransitioning(bool value)
    {
        transitioning = value;
        NavigateCommand.NotifyCanExecuteChanged();
        SelectLocationCommand.NotifyCanExecuteChanged();
        SaveSettingsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>停止接受頁面操作、取消並等待進行中的工作，最後停止子頁面 ViewModel。</summary>
    public async Task StopAsync()
    {
        closing = true;
        SetTransitioning(true);
        await lifetime.CancelAsync();
        try
        {
            await Task.WhenAll(initialize ?? Task.CompletedTask, NavigateCommand.ExecutionTask ?? Task.CompletedTask,
                SelectLocationCommand.ExecutionTask ?? Task.CompletedTask, SaveSettingsCommand.ExecutionTask ?? Task.CompletedTask);
        }
        finally
        {
            await Task.WhenAll(Dashboard.StopAsync(), Home.StopAsync());
            lifetime.Dispose();
        }
    }
}
