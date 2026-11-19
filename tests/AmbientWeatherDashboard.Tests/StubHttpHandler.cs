namespace AmbientWeatherDashboard.Tests;

/// <summary>將測試提供的非同步委派作為 HttpClient 的傳輸處理器。</summary>
internal sealed class StubHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    : HttpMessageHandler
{
    /// <summary>把 HTTP 請求及取消權杖轉交給測試委派處理。</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        send(request, cancellationToken);
}
