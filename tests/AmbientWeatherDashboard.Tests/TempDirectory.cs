namespace AmbientWeatherDashboard.Tests;

/// <summary>提供測試專用的唯一暫存目錄，並在結束時清除其內容。</summary>
internal sealed class TempDirectory : IDisposable
{
    /// <summary>目前測試使用的暫存目錄路徑。</summary>
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AmbientWeatherTests", Guid.NewGuid().ToString("N"));

    /// <summary>若暫存目錄存在，遞迴刪除該目錄及其中的測試檔案。</summary>
    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
