using System.Diagnostics;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

public sealed class SettingsService(string appDataDirectory)
{
    private readonly string path = Path.Combine(appDataDirectory, "settings.json");

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await AppFiles.ReadAsync<AppSettings>(path, cancellationToken).ConfigureAwait(false);
        if (settings?.Location is { IsValid: true })
            return settings;
        if (settings is not null)
            Trace.TraceWarning("Settings contained an invalid location. Using Taipei.");
        return settings is null ? AppSettings.Default : settings with { Location = LocationInfo.Taipei };
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (!settings.Location.IsValid)
            throw new ArgumentException("Invalid settings location.", nameof(settings));

        return AppFiles.WriteAsync(path, settings, cancellationToken);
    }
}
