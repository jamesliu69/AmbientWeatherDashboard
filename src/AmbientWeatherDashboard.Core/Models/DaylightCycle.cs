namespace AmbientWeatherDashboard.Core.Models;

public readonly record struct DaylightCycle(double StartAngle, double Sweep, double? Progress, double? Seconds,
    DateTimeOffset? Sunrise, DateTimeOffset? Sunset);
