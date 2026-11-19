namespace AmbientWeatherDashboard.Tests;

internal sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ForecastFixture.Now;
}
