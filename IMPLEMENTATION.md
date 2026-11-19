# 實作順序與偽程式碼

1. **視覺骨架**。建立 .NET 10 Solution、WPF 應用程式和平台無關的 Core。WindowChrome 提供系統視窗操作。Dashboard 以四個等寬 Grid 欄位配置七張獨立卡片，卡片各自安排標題、內容與選單。設計器可以載入版型，正式初始畫面以破折號表示缺值。交叉編譯 XAML。
2. **向量與動畫**。控制項在各自的可用空間內置中縮放向量。羅盤先正規化角度，再沿最短弧旋轉。濕度與垂直分段用實際比例填色。日照環以地方時間轉為角度，進度以 UTC 經過時間計算。用有限長的動畫更新圖形，雲朵只在可見且允許動畫時移動。重新建置。
3. **真實資料**。用 HTTP 取得目前、逐小時、逐日資料。解析時間戳，以官方時區轉換時鐘與比較日期。檢查每個欄位，保留支援其餘卡片的有效資料。由資料時間取得對應逐時樣本，從同一地點的逐日資料取昨日最高溫與今日 UV 峰值。成功後以原子替換寫入依座標區隔的 JSON 快取。
4. **互動**。載入已保存設定 → 顯示城市 → 讀取快取 → 啟動非同步更新。每 15 分鐘與手動更新走同一排程，重複請求共用進行中的工作。城市變更時先取消舊工作再重新載入。Home 負責搜尋，設定畫面保存單位、語言及動畫偏好。關閉時停止計時、取消並等待工作完成。
5. **驗證**。建置全 Solution，測試 API 資料邊界、日照、DST、設定、快取與取消。用真實端點確認欄位與更新路徑。在可用的 Windows 桌面上檢查視窗、動畫、語言與 DPI。

```text
refresh(location, cancellation):
    join an existing refresh for the same location
    try:
        response = fetch current + hourly + daily series in one HTTP request
        snapshot = validate response and select present weather fields
        cache.save(location, snapshot.source, fetchedAt)
        display snapshot as connected
    on recoverable failure:
        display last valid snapshot for location as cached, or display no data
    always:
        clear pending refresh so the next request can run

display(snapshot, now, settings):
    localNow = convert(now, snapshot.timeZone or location.timeZone)
    hours = first five forecast timestamps strictly after now
    today = daily row matching localNow.date or the active adjacent daylight interval
    yesterday = daily row matching localNow.date - one calendar day
    dayArc = normalized clock angle at sunrise plus clockwise sweep to sunset
    progress = clamp((now - sunrise) / (sunset - sunrise), 0, 1)
    if polar day/night: use explicit full/empty states from daylight duration
    apply settings to strings and values; update only changed properties and rows
```
