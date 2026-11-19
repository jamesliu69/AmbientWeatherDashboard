using System.IO;
using System.Net.Http;
using System.Windows;
using AmbientWeatherDashboard.Core.Services;
using AmbientWeatherDashboard.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AmbientWeatherDashboard;

/// <summary>建立應用程式服務及主視窗，並在結束時釋放服務提供者。</summary>
public partial class App : Application
{
    /// <summary>保存應用程式啟動時建立的相依性服務提供者。</summary>
    private ServiceProvider? services;

    /// <summary>註冊應用程式服務與 ViewModel，建立相依性容器並顯示主視窗。</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AmbientWeatherDashboard");
        var registrations = new ServiceCollection();
        registrations.AddSingleton(TimeProvider.System);
        registrations.AddSingleton(new SettingsService(directory));
        registrations.AddSingleton(new WeatherCacheService(directory));
        registrations.AddHttpClient<OpenMeteoService>(client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);
        registrations.AddHttpClient<GeocodingService>(client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);
        registrations.AddSingleton<WeatherRefreshService>();
        registrations.AddSingleton<HomeViewModel>();
        registrations.AddSingleton<DashboardViewModel>();
        registrations.AddSingleton<SettingsViewModel>();
        registrations.AddSingleton<MainViewModel>();
        registrations.AddSingleton<MainWindow>();
        services = registrations.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        MainWindow = services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    /// <summary>建立連線池生命週期為五分鐘的 SocketsHttpHandler。</summary>
    private static SocketsHttpHandler CreateHandler() => new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    /// <summary>釋放相依性服務提供者，再交由 WPF 執行基底結束處理。</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        services?.Dispose();
        base.OnExit(e);
    }
}
