using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AmbientWeatherDashboard.ViewModels;

public sealed partial class HomeViewModel(GeocodingService geocoding) : ObservableObject
{
    private bool chinese;
    private UiText text = new(false);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string query = "";

    [ObservableProperty] private string searchStatus = "";
    [ObservableProperty] private string? searchError;
    [ObservableProperty] private IReadOnlyList<LocationInfo> results = [];
    [ObservableProperty] private LocationInfo? selectedLocation;

    public void Configure(AppSettings settings)
    {
        chinese = settings.TraditionalChinese;
        text = new UiText(chinese);
        SearchStatus = "";
        SearchError = null;
    }

    private bool CanSearch() => Query.Trim().Length is >= 2 and <= 100;

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

    public async Task StopAsync()
    {
        SearchCommand.Cancel();
        if (SearchCommand.ExecutionTask is { } task)
            await task;
    }
}
