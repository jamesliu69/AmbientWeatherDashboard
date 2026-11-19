# Ambient Weather Dashboard

以 .NET 10 與 WPF 製作的 Windows 氣象桌面儀表板。預設顯示台北與英文介面。從 **Back to Home** 搜尋城市，從 **Settings** 切換繁體中文、°C／°F、KM/H／MPH 及減少動態效果。按 **Refresh weather**、F5 或卡片選單可手動更新。

## 在 Windows 執行

使用支援 .NET 10 的 Windows 10／11 版本與 [.NET 10 SDK](https://learn.microsoft.com/dotnet/core/install/windows)。使用 Visual Studio 時選擇 Visual Studio 2026 18.0 以上及「.NET 桌面開發」工作負載。Microsoft 目前支援的 Windows 10 版本以 LTSC／Enterprise 為主。Windows 10 與 Windows 11 的實際相容性仍需依下方矩陣驗收。

```powershell
dotnet build AmbientWeatherDashboard.sln -c Release -warnaserror
dotnet run --project src/AmbientWeatherDashboard -c Release
```

也可在 Windows 發行 x64 版本。應用程式所在電腦需要同架構的 .NET 10 **Desktop Runtime**。

```powershell
dotnet publish src/AmbientWeatherDashboard -c Release -r win-x64 --self-contained false -o artifacts/win-x64
.\artifacts\win-x64\AmbientWeatherDashboard.exe
```

## 資料與計算

| 卡片 | 資料與顯示 |
| --- | --- |
| TODAY | 所選城市的即時鐘、最新模型天氣與日夜向量圖示 |
| FORECAST | 以當地時間顯示現在之後五筆逐時預報。半小時／四分之一小時時區保留換算後的分鐘數。10 公尺風速達 40 km/h 時附加風勢符號 |
| WIND | 10 公尺風速、陣風與氣象風向。指針指向風的來源 |
| HUMIDITY | 目前相對濕度。差異以當地連續兩天同一小時的逐時濕度相減，單位為百分點 |
| SUN | 前一小時平均太陽輻射、今日逐時輻射峰值、UV 等級及當日最大 UV。估計光照指數以 0–1000 W/m² 對應 0–100，上限為 100 |
| TEMPERATURE | 即時溫度、逐時露點、昨日最高溫及體感溫度 |
| SUN CYCLE | 以日出日落繪製 24 小時圓環，顯示 API 白晝秒數與已經過的白晝區間，涵蓋跨午夜、永晝與極夜 |

若 API 固定時差的逐日邊界跨越夏令時間轉換，日最高溫及 UV 峰值改用當地日期內完整的逐時資料計算。

氣象模式資料來自 [Open‑Meteo Forecast API](https://open-meteo.com/en/docs)，城市來自 [Open‑Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)。免費端點的使用條件見 [Open‑Meteo 定價與使用條件](https://open-meteo.com/en/pricing)。

啟動或返回 Dashboard 時會更新資料。停留於 Dashboard 時每 15 分鐘自動更新。網路中斷時會回退至同一城市的最近一次有效回應，顯示 **Offline · Cached** 與更新時間。若城市缺少可用快取，顯示 **No Data**。欄位缺值時以破折號顯示。設定存於 `%LOCALAPPDATA%\AmbientWeatherDashboard\settings.json`，各城市快取存於同目錄的 `weather-cache` 資料夾。

## 專案結構

- `src/AmbientWeatherDashboard` 包含 WPF 視窗、Home／Dashboard／Settings、七張卡片、向量控制項、主題、ViewModel 及 DI 註冊。
- `src/AmbientWeatherDashboard.Core` 包含 Open‑Meteo 用戶端、資料模型、時區與儀表運算、更新協調、設定與快取。
- `tests/AmbientWeatherDashboard.Tests` 包含可跨平台執行的單元測試，以及連線至真實 Open‑Meteo 的整合測試。
- [實作順序與偽程式碼](IMPLEMENTATION.md)。

## 驗證

```powershell
dotnet test AmbientWeatherDashboard.sln -c Release --filter "Category!=Live"
dotnet test AmbientWeatherDashboard.sln -c Release --filter "Category=Live"
```

單元測試使用本機測試資料與暫存目錄。Live 測試會連線至 Open‑Meteo，搜尋台北、讀取預報並重讀本機快取。

### Windows 桌面驗收矩陣

| 項目 | 檢查內容 |
| --- | --- |
| 1280 × 800、1600 × 960、1920 × 1080 | 四欄七卡、卡片標題與數值無重疊或裁切。羅盤及圓環維持正圓。一般最小視窗為 1280 × 760 DIP。高 DPI 下若工作區較小，視窗會縮至可用工作區，可用捲動到達卡片。 |
| 125%、150%、200% DPI | 依序切換 Windows 縮放，檢查視窗拖曳、邊界縮放、雙擊最大化及還原。搭配多螢幕確認 PerMonitorV2。 |
| 英文與繁體中文、°C 與 °F、KM/H 與 MPH | 溫度主值、露點、體感及逐時預報使用同一溫標。所有主要按鈕、設定與卡片標題隨語言切換。重啟後確認設定保存。 |
| 卡片與動畫 | 滑過卡片確認亮度、邊框與 Tooltip。逐卡操作三點選單。手動更新時檢查羅盤、濕度環與分段柱。最小化和減少動態效果時確認動畫停止。 |
| 網路中斷、恢復及退出 | 先取得資料，再中斷連線並更新。確認快取／無資料狀態。恢復連線再更新。分別於搜尋與更新中返回頁面或關閉視窗。 |

目前工作區是 Linux ARM64，可交叉編譯 WPF 並執行核心測試。上表的 GUI、DPI 與截圖比對需在 Windows 桌面執行。
