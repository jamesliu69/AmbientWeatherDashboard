namespace AmbientWeatherDashboard.ViewModels;

public sealed record ForecastTile(DateTimeOffset Timestamp, string Time, int? Code, bool? IsDay, bool IsWindy, string Temperature, string Caption);
