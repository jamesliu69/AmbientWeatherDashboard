using AmbientWeatherDashboard.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AmbientWeatherDashboard.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool fahrenheit;
    [ObservableProperty] private bool milesPerHour;
    [ObservableProperty] private bool traditionalChinese;
    [ObservableProperty] private bool reducedMotion;

    public void Configure(AppSettings settings)
    {
        Fahrenheit = settings.Fahrenheit;
        MilesPerHour = settings.MilesPerHour;
        TraditionalChinese = settings.TraditionalChinese;
        ReducedMotion = settings.ReducedMotion;
    }

    public AppSettings ToSettings(LocationInfo location) => new(location, Fahrenheit, MilesPerHour, TraditionalChinese, ReducedMotion);
}
