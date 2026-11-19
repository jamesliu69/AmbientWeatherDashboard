using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;

namespace AmbientWeatherDashboard.Core.Services;

public sealed record WeatherPayload(DateTimeOffset FetchedAt, JsonElement Source, WeatherSnapshot Snapshot);
