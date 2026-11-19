using System.Globalization;

namespace AmbientWeatherDashboard.ViewModels;

/// <summary>依介面語言提供本地化文字、天氣狀況及時長格式化結果。</summary>
public sealed class UiText(bool chinese)
{
    public bool IsChinese => chinese;
    public CultureInfo Culture => CultureInfo.GetCultureInfo(chinese ? "zh-TW" : "en-US");
    public string TodayTitle => Pick("TODAY", "今日");
    public string ForecastTitle => Pick("FORECAST", "逐時預報");
    public string WindTitle => Pick("WIND", "風況");
    public string HumidityTitle => Pick("HUMIDITY", "相對濕度");
    public string SunTitle => Pick("SUN", "太陽輻射與紫外線");
    public string TemperatureTitle => Pick("TEMPERATURE", "溫度");
    public string SunCycleTitle => Pick("SUN CYCLE", "日照週期");
    public string TodayTip => Pick("Local time and the latest model weather for this city.", "所選城市的當地時間與最新模型天氣。");
    public string ForecastTip => Pick("The next five hourly model samples, shown in local time.", "以當地時間顯示接下來五筆逐時模型預報。");
    public string WindTip => Pick("The compass shows where the wind comes from. Gust is the latest model gust at 10 meters.", "羅盤指向風的來向。陣風為最新模型在 10 公尺高度的陣風。");
    public string HumidityTip => Pick("Current relative humidity. The comparison uses hourly humidity for the same local hour on consecutive dates.", "目前相對濕度。比較值取自連續兩天同一當地小時的逐時濕度。");
    public string SunTip => Pick("Radiation is a preceding-hour mean. Peaks use today's model data. The light index scales 0–1000 W/m² to 0–100.", "輻射為前一小時平均。峰值取自今日模型資料。光照指數把 0–1000 W/m² 對應至 0–100。");
    public string TemperatureTip => Pick("Current temperature, hourly dew point, yesterday’s daily high and current apparent temperature.", "目前氣溫、逐時露點、昨日最高溫與目前體感溫度。");
    public string SunCycleTip => Pick("The gold arc marks daylight on a 24-hour dial. The blue arc follows elapsed daylight.", "金線標示 24 小時刻度中的白晝區間，藍線顯示已度過的白晝。");
    public string WindNow => Pick("WIND SPEED", "目前風速");
    public string Gust => Pick("GUST", "陣風");
    public string YesterdaySameHour => Pick("vs. yesterday, same local hour", "與昨日同一當地小時相比");
    public string DewPoint => Pick("DEW POINT", "露點");
    public string YesterdayHigh => Pick("YESTERDAY HIGH", "昨日最高溫");
    public string FeelsLike => Pick("FEELS LIKE", "體感溫度");
    public string Sunrise => Pick("Sunrise", "日出");
    public string Sunset => Pick("Sunset", "日落");
    public string Solar => Pick("Solar radiation", "太陽輻射");
    public string Uv => Pick("UV Index", "紫外線指數");
    public string Light => Pick("Estimated Light Index", "估計光照指數");
    public string Peak => Pick("Today’s peak", "今日峰值");
    public string Relative => Pick("Relative scale · 0–100", "相對指數 · 0–100");
    public string Offline => Pick("Offline · Cached", "離線 · 快取");
    public string NoData => Pick("No Data", "無資料");
    public string Connected => Pick("Connected", "已連線");
    public string Updating => Pick("Updating…", "更新中…");
    public string LastUpdated => Pick("Updated", "更新時間");
    public string CacheFailed => Pick("Weather loaded. Could not write local cache.", "已載入天氣資料，但無法寫入本機快取。");
    public string NoForecast => Pick("No upcoming data", "無後續預報資料");
    public string ForecastUpdateFailed => Pick("Weather refresh failed.", "天氣更新失敗。");
    public string Home => Pick("Back to Home", "返回首頁");
    public string Dashboard => Pick("Open Dashboard", "開啟儀表板");
    public string Settings => Pick("Settings", "設定");
    public string Refresh => Pick("Refresh weather", "更新天氣");
    public string Details => Pick("Card details", "卡片詳細資訊");
    public string Source => Pick("Weather data · Open-Meteo", "氣象資料 · Open-Meteo");
    public string CitySearch => Pick("Find a city", "搜尋城市");
    public string CityQuery => Pick("City name", "城市名稱");
    public string Search => Pick("Search", "搜尋");
    public string Searching => Pick("Searching…", "搜尋中…");
    public string NoCities => Pick("No matching cities.", "找不到符合的城市。");
    public string SearchFailed => Pick("City search failed.", "城市搜尋失敗。");
    public string UseCity => Pick("Use selected city", "使用選取的城市");
    public string CurrentCity => Pick("Current city", "目前城市");
    public string SaveFailed => Pick("Could not save settings.", "無法儲存設定。");
    public string SettingsIntro => Pick("Weather display", "氣象顯示");
    public string Fahrenheit => Pick("Show temperatures in Fahrenheit (°F)", "以華氏顯示溫度（°F）");
    public string Miles => Pick("Show wind speed in miles per hour (MPH)", "以英里／小時顯示風速（MPH）");
    public string Chinese => Pick("繁體中文介面 / Traditional Chinese", "繁體中文介面 / Traditional Chinese");
    public string Reduced => Pick("Reduce motion", "減少動態效果");
    public string Apply => Pick("Save settings", "儲存設定");
    public string Cancel => Pick("Cancel", "取消");
    public string Minimize => Pick("Minimize", "最小化");
    public string Maximize => Pick("Maximize or restore", "最大化或還原");
    public string Close => Pick("Close", "關閉");
    public string Clear => Pick("Clear", "晴朗");
    public string Cloudy => Pick("Partly cloudy", "局部多雲");
    public string Overcast => Pick("Overcast", "陰天");
    public string Fog => Pick("Fog", "霧");
    public string Rain => Pick("Rain", "雨");
    public string Snow => Pick("Snow", "雪");
    public string Windy => Pick("Windy", "風勢強");
    public string Storm => Pick("Thunderstorm", "雷雨");
    public string Day => Pick("day", "白天");
    public string Night => Pick("night", "夜晚");
    public string PolarDay => Pick("Polar day", "永晝");
    public string PolarNight => Pick("Polar night", "極夜");

    /// <summary>依紫外線級別回傳對應的英文或繁體中文名稱。</summary>
    public string UvLevel(int level) => level switch
    {
        0 => Pick("Low", "低量級"),
        1 => Pick("Moderate", "中量級"),
        2 => Pick("High", "高量級"),
        3 => Pick("Very High", "過量級"),
        _ => Pick("Extreme", "危險級")
    };

    /// <summary>依天氣代碼與白晝狀態組合目前天氣描述。</summary>
    public string Condition(int? weatherCode, bool? daylight)
    {
        var description = weatherCode switch
        {
            null => NoData,
            0 => Clear,
            1 or 2 => Cloudy,
            3 => Overcast,
            45 or 48 => Fog,
            71 or 73 or 75 or 77 or 85 or 86 => Snow,
            >= 95 => Storm,
            _ => Rain
        };
        return weatherCode is <= 2 && daylight is { } isDay ? $"{description} · {(isDay ? Day : Night)}" : description;
    }

    /// <summary>將日照秒數格式化為本地語言的時長，並處理永晝與極夜。</summary>
    public string DaylightDuration(double seconds)
    {
        if (seconds == 86400)
            return PolarDay;
        if (seconds == 0)
            return PolarNight;
        var minutes = (int)Math.Round(seconds / 60);
        return chinese ? $"{minutes / 60} 小時 {minutes % 60} 分" : $"{minutes / 60} hr {minutes % 60} min";
    }

    /// <summary>依介面語言格式化相對濕度差的百分點文字。</summary>
    public string PercentagePoints(double value) =>
        value.ToString("+0;-0;0", Culture) + Pick(" pp", " 個百分點");

    /// <summary>依目前語言選取英文或繁體中文文字。</summary>
    private string Pick(string english, string traditionalChinese) => chinese ? traditionalChinese : english;
}
