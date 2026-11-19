using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>保存擷取時間、原始 JSON 回應及解析後的天氣快照。</summary>
public sealed record WeatherPayload(DateTimeOffset FetchedAt, JsonElement Source, WeatherSnapshot Snapshot);
