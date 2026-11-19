namespace AmbientWeatherDashboard.Core.Models;

/// <summary>描述日照弧線的起始角度、掃掠角、目前進度與日出日落時間。</summary>
public readonly record struct DaylightCycle(double StartAngle, double Sweep, double? Progress, double? Seconds,
    DateTimeOffset? Sunrise, DateTimeOffset? Sunset);
