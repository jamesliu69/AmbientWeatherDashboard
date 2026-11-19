using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmbientWeatherDashboard.ViewModels;

/// <summary>管理地點搜尋輸入、搜尋結果與選取地點狀態。</summary>
public sealed partial class HomeViewModel(GeocodingService geocoding) : ObservableObject
{
    /// <summary>記錄目前搜尋及介面文字是否使用繁體中文。</summary>
    private bool chinese;
    /// <summary>保存目前語言的首頁與搜尋狀態文字。</summary>
    private UiText text = new(false);

    /// <summary>保存地點搜尋文字，並由來源產生器提供 Query 屬性。</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string query = "";

    /// <summary>保存目前的搜尋進度或結果狀態文字。</summary>
    [ObservableProperty] private string searchStatus = "";
    /// <summary>保存地點搜尋失敗時的錯誤細節。</summary>
    [ObservableProperty] private string? searchError;
    /// <summary>保存最近一次搜尋取得的有效地點。</summary>
    [ObservableProperty] private IReadOnlyList<LocationInfo> results = [];
    /// <summary>保存目前選取的搜尋結果。</summary>
    [ObservableProperty] private LocationInfo? selectedLocation;

    /// <summary>依使用者偏好更新語言，並清除先前搜尋狀態與錯誤。</summary>
    public void Configure(AppSettings settings)
    {
        chinese = settings.TraditionalChinese;
        text = new UiText(chinese);
        SearchStatus = "";
        SearchError = null;
    }

    /// <summary>只在搜尋文字去除空白後長度介於 2 至 100 個字元時允許搜尋。</summary>
    private bool CanSearch() => Query.Trim().Length is >= 2 and <= 100;

    /// <summary>查詢地點並顯示結果；輸入已變更時略過舊結果，錯誤則更新狀態文字。</summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        var requested = Query;
        SearchStatus = text.Searching;
        SearchError = null;
        SelectedLocation = null;
        Results = [];
        try
        {
            var result = await geocoding.SearchAsync(requested, chinese, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(requested.Trim(), Query.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                SearchStatus = "";
                return;
            }
            Results = result;
            SelectedLocation = result.FirstOrDefault();
            SearchStatus = result.Count == 0 ? text.NoCities : "";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SearchStatus = "";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or JsonException or TimeoutException)
        {
            Trace.TraceWarning("City search failed: {0}", ex.Message);
            SearchStatus = text.SearchFailed;
            SearchError = ex.Message;
        }
    }

    /// <summary>取消地點搜尋命令並等待其執行工作結束。</summary>
    public async Task StopAsync()
    {
        SearchCommand.Cancel();
        if (SearchCommand.ExecutionTask is { } task)
            await task;
    }
}
