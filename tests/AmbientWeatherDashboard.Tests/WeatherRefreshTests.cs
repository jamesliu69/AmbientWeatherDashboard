using System.Net;
using AmbientWeatherDashboard.Core.Models;
using AmbientWeatherDashboard.Core.Services;

namespace AmbientWeatherDashboard.Tests;

public sealed class WeatherRefreshTests
{
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

    private static WeatherRefreshService Create(HttpClient client, TempDirectory temp) =>
        new(new OpenMeteoService(client, new FixedClock()), new WeatherCacheService(temp.Path));

    private static HttpResponseMessage ForecastResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent(ForecastFixture.Json()) };
}
