using System.IO;
using System.Net.Http;
using System.Windows;
using AmbientWeatherDashboard.Core.Services;
using AmbientWeatherDashboard.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AmbientWeatherDashboard;

public partial class App : Application
{
    private ServiceProvider? services;

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

    private static SocketsHttpHandler CreateHandler() => new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    protected override void OnExit(ExitEventArgs e)
    {
        services?.Dispose();
        base.OnExit(e);
    }
}
