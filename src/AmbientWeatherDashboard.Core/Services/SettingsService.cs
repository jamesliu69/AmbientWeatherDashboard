using System.Diagnostics;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>從應用程式資料目錄載入及儲存使用者設定。</summary>
public sealed class SettingsService(string appDataDirectory)
{
    /// <summary>保存設定 JSON 檔案在應用程式資料目錄中的路徑。</summary>
    private readonly string path = Path.Combine(appDataDirectory, "settings.json");

    /// <summary>載入設定；資料不存在時使用預設值，位置無效時改用臺北。</summary>
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await AppFiles.ReadAsync<AppSettings>(path, cancellationToken).ConfigureAwait(false);
        if (settings?.Location is { IsValid: true })
            return settings;
        if (settings is not null)
            Trace.TraceWarning("Settings contained an invalid location. Using Taipei.");
        return settings is null ? AppSettings.Default : settings with { Location = LocationInfo.Taipei };
    }

    /// <summary>驗證設定中的地點後，將設定寫入 JSON 檔案。</summary>
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (!settings.Location.IsValid)
            throw new ArgumentException("Invalid settings location.", nameof(settings));

        return AppFiles.WriteAsync(path, settings, cancellationToken);
    }
}
