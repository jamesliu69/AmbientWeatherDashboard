using System.Net;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;

namespace AmbientWeatherDashboard.Tests;

/// <summary>驗證天氣更新共用、取消、失敗重試與快取回復流程。</summary>
public sealed class WeatherRefreshTests
{
    /// <summary>檢查同地點請求共用一次 HTTP 工作，加入者取消不會取消擁有者。</summary>
    [Fact]
    public async Task ConcurrentRefreshesShareOneRequestAndCancelingAJoinerDoesNotStopTheOwner()
    {
        using var temp = new TempDirectory();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var joining = new CancellationTokenSource();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        using var client = new HttpClient(new StubHttpHandler(async (_, token) =>
        {
            Interlocked.Increment(ref requests);
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            return ForecastResponse();
        }));
        var service = Create(client, temp);

        var owner = service.RefreshAsync(LocationInfo.Taipei, timeout.Token);
        await entered.Task.WaitAsync(timeout.Token);
        var join = service.RefreshAsync(LocationInfo.Taipei, joining.Token);
        var other = service.RefreshAsync(LocationInfo.Taipei, timeout.Token);
        await joining.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => join);
        release.TrySetResult();
        Assert.Same(await owner, await other);
        Assert.Equal(1, requests);
    }

    /// <summary>檢查取消擁有者會終止 HTTP 工作，後續更新仍能重新發出請求。</summary>
    [Fact]
    public async Task CancelingTheOwnerAbortsHttpAndAllowsTheNextRefresh()
    {
        using var temp = new TempDirectory();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var owner = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        using var client = new HttpClient(new StubHttpHandler(async (_, token) =>
        {
            if (Interlocked.Increment(ref count) == 1)
            {
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                finally
                {
                    canceled.TrySetResult();
                }
            }
            return ForecastResponse();
        }));
        var service = Create(client, temp);

        var running = service.RefreshAsync(LocationInfo.Taipei, owner.Token);
        await entered.Task.WaitAsync(timeout.Token);
        await owner.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        Assert.True(canceled.Task.IsCompleted);
        Assert.True((await service.RefreshAsync(LocationInfo.Taipei, timeout.Token)).Connected);
        Assert.Equal(2, count);
    }

    /// <summary>檢查網路中斷時使用最近快取，服務恢復後可再次取得新資料。</summary>
    [Fact]
    public async Task NetworkFailureUsesLastGoodDataAndRefetchesAfterRecovery()
    {
        using var temp = new TempDirectory();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var online = false;
        var requestCount = 0;
        using var client = new HttpClient(new StubHttpHandler((_, _) =>
        {
            Interlocked.Increment(ref requestCount);
            return online ? Task.FromResult(ForecastResponse())
                : Task.FromException<HttpResponseMessage>(new HttpRequestException("Network unavailable"));
        }));
        var service = Create(client, temp);

        var empty = await service.RefreshAsync(LocationInfo.Taipei, timeout.Token);
        Assert.False(empty.Connected);
        Assert.Null(empty.Snapshot);
        Assert.Equal(3, requestCount);
        online = true;
        Assert.True((await service.RefreshAsync(LocationInfo.Taipei, timeout.Token)).Connected);
        online = false;
        var offline = await service.RefreshAsync(LocationInfo.Taipei, timeout.Token);
        Assert.False(offline.Connected);
        Assert.Equal(23, offline.Snapshot?.Current.Temperature);
        Assert.NotNull(offline.Error);
        online = true;
        Assert.True((await service.RefreshAsync(LocationInfo.Taipei, timeout.Token)).Connected);
    }

    /// <summary>檢查永久性 HTTP 錯誤不會重試。</summary>
    [Fact]
    public async Task PermanentHttpErrorsDoNotRetry()
    {
        using var temp = new TempDirectory();
        var requests = 0;
        using var client = new HttpClient(new StubHttpHandler((_, _) =>
        {
            Interlocked.Increment(ref requests);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }));
        var result = await Create(client, temp).RefreshAsync(LocationInfo.Taipei, default);
        Assert.False(result.Connected);
        Assert.Equal(1, requests);
    }

    /// <summary>以測試 HTTP 用戶端與暫存快取建立天氣更新服務。</summary>
    private static WeatherRefreshService Create(HttpClient client, TempDirectory temp) =>
        new(new OpenMeteoService(client, new FixedClock()), new WeatherCacheService(temp.Path));

    /// <summary>以成功狀態碼及共用預報 JSON 建立 HTTP 回應。</summary>
    private static HttpResponseMessage ForecastResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent(ForecastFixture.Json()) };
}
