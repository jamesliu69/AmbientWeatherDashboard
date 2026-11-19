namespace AmbientWeatherDashboard.Core.Models;

public sealed record AppSettings(LocationInfo Location, bool Fahrenheit = false, bool MilesPerHour = false,
    bool TraditionalChinese = false, bool ReducedMotion = false)
{
    public static readonly AppSettings Default = new(LocationInfo.Taipei);
}
