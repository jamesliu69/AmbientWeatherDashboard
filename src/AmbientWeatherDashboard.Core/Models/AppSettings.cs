namespace AmbientWeatherDashboard.Core.Models;

/// <summary>保存儀表板使用的位置、單位、語言與動態效果偏好。</summary>
public sealed record AppSettings(LocationInfo Location, bool Fahrenheit = false, bool MilesPerHour = false,
    bool TraditionalChinese = false, bool ReducedMotion = false)
{
    /// <summary>提供以臺北為預設位置的應用程式設定。</summary>
    public static readonly AppSettings Default = new(LocationInfo.Taipei);
}
