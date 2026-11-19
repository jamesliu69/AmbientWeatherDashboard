using System.IO;
using System.Diagnostics;
using System.Windows;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmbientWeatherDashboard.ViewModels;

public sealed partial class MainViewModel(SettingsService settingsService, DashboardViewModel dashboard,
    HomeViewModel home, SettingsViewModel settings) : ObservableObject
{
    private readonly CancellationTokenSource lifetime = new();
    private AppSettings preferences = AppSettings.Default;
    private bool transitioning = true;
    private bool closing;
    private bool initialized;
    private Task? initialize;

    public DashboardViewModel Dashboard { get; } = dashboard;
    public HomeViewModel Home { get; } = home;
    public SettingsViewModel Settings { get; } = settings;
    public UiText Text => Dashboard.Text;
    public Visibility DashboardVisibility => initialized && CurrentPage == Page.Dashboard ? Visibility.Visible : Visibility.Collapsed;
    public Visibility HomeVisibility => CurrentPage == Page.Home ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SettingsShortcutVisibility => CurrentPage == Page.Settings ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SettingsVisibility => CurrentPage == Page.Settings ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DashboardVisibility), nameof(HomeVisibility), nameof(SettingsVisibility), nameof(SettingsShortcutVisibility))]
    private Page currentPage = Page.Dashboard;

    [ObservableProperty] private string? error;
    [ObservableProperty] private string? errorDetail;

    public Task StartAsync() => initialize ??= InitializeAsync();

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
        }
        finally
        {
            SetTransitioning(false);
        }
    }

    private void ApplyPreferences()
    {
        Dashboard.Configure(preferences);
        Home.Configure(preferences);
        Settings.Configure(preferences);
        OnPropertyChanged(nameof(Text));
    }

    private bool CanNavigate(Page page) => !transitioning && !closing && CurrentPage != page;

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private Task NavigateAsync(Page page) => TransitionAsync(page, null);

    private bool CanSelectLocation(LocationInfo? location) => !transitioning && !closing && location is { IsValid: true };

    [RelayCommand(CanExecute = nameof(CanSelectLocation))]
    private Task SelectLocationAsync(LocationInfo? location) => location is null
        ? Task.CompletedTask
        : TransitionAsync(Page.Dashboard, preferences with { Location = location });

    private bool CanSaveSettings() => !transitioning && !closing;

    [RelayCommand(CanExecute = nameof(CanSaveSettings))]
    private Task SaveSettingsAsync() => TransitionAsync(Page.Dashboard, Settings.ToSettings(preferences.Location));

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
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("Could not save settings: {0}", ex.Message);
            Error = Text.SaveFailed;
            ErrorDetail = ex.Message;
        }
        finally
        {
            SetTransitioning(false);
        }
    }

    private void SetTransitioning(bool value)
    {
        transitioning = value;
        NavigateCommand.NotifyCanExecuteChanged();
        SelectLocationCommand.NotifyCanExecuteChanged();
        SaveSettingsCommand.NotifyCanExecuteChanged();
    }

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
