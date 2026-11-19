using AmbientWeatherDashboard.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AmbientWeatherDashboard.ViewModels;

/// <summary>保存設定頁的單位、語言及動態效果選項。</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    /// <summary>記錄是否以華氏顯示溫度。</summary>
    [ObservableProperty] private bool fahrenheit;
    /// <summary>記錄是否以英里每小時顯示風速。</summary>
    [ObservableProperty] private bool milesPerHour;
    /// <summary>記錄是否使用繁體中文介面。</summary>
    [ObservableProperty] private bool traditionalChinese;
    /// <summary>記錄是否減少介面動態效果。</summary>
    [ObservableProperty] private bool reducedMotion;

    /// <summary>將既有偏好載入設定頁的可繫結選項。</summary>
    public void Configure(AppSettings settings)
    {
        Fahrenheit = settings.Fahrenheit;
        MilesPerHour = settings.MilesPerHour;
        TraditionalChinese = settings.TraditionalChinese;
        ReducedMotion = settings.ReducedMotion;
    }

    /// <summary>以指定地點及目前選項建立新的應用程式設定記錄。</summary>
    public AppSettings ToSettings(LocationInfo location) => new(location, Fahrenheit, MilesPerHour, TraditionalChinese, ReducedMotion);
}
