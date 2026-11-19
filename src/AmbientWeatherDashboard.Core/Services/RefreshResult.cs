using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

public sealed record RefreshResult(WeatherSnapshot? Snapshot, bool Connected, string? Error = null, bool CacheWriteFailed = false);
