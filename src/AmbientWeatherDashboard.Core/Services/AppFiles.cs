using System.Diagnostics;
using System.Text.Json;

namespace AmbientWeatherDashboard.Core.Services;

/// <summary>提供應用程式 JSON 檔案的非同步讀取與暫存檔寫入。</summary>
internal static class AppFiles
{
    /// <summary>反序列化 JSON 檔案；檔案不存在時回傳 default，JSON、I/O 或權限錯誤會記錄警告後回傳 default。</summary>
    internal static async Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return default;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("Unable to read {0}: {1}", path, ex.Message);
            return default;
        }
    }

    /// <summary>將資料序列化至暫存檔，再覆寫目標檔並嘗試清除暫存檔。</summary>
    internal static async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, value, cancellationToken: cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning("Unable to remove {0}: {1}", temporary, ex.Message);
            }
        }
    }
}
