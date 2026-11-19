namespace AmbientWeatherDashboard.Tests;

/// <summary>回傳固定測試時間的 TimeProvider。</summary>
internal sealed class FixedClock : TimeProvider
{
    /// <summary>回傳 ForecastFixture 所定義的 UTC 測試時間。</summary>
    public override DateTimeOffset GetUtcNow() => ForecastFixture.Now;
}
