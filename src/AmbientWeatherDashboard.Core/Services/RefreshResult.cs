using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>回報天氣快照、網路連線狀態、錯誤訊息與快取寫入結果。</summary>
public sealed record RefreshResult(WeatherSnapshot? Snapshot, bool Connected, string? Error = null, bool CacheWriteFailed = false);
