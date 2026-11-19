namespace AmbientWeatherDashboard.ViewModels;

/// <summary>保存單筆逐時預報的時間、天氣代碼、風況及顯示文字。</summary>
public sealed record ForecastTile(DateTimeOffset Timestamp, string Time, int? Code, bool? IsDay, bool IsWindy, string Temperature, string Caption);
