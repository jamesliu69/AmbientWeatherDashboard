using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace AmbientWeatherDashboard.Core.Services;

internal static class OpenMeteoHttp
{
    internal static async Task<JsonElement> GetJsonAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var delay = TimeSpan.FromMilliseconds(350 * (1 << attempt));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                    .ConfigureAwait(false);
                var retryAfter = response.Headers.RetryAfter;
                var requested = retryAfter?.Delta ?? (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
                if (requested is { } retryDelay && retryDelay > delay)
                    delay = retryDelay;
                if (delay > TimeSpan.FromSeconds(4))
                    attempt = 2;

                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < 2 && IsTransient(ex.StatusCode))
            {
                Trace.TraceWarning("Open-Meteo attempt {0} failed: {1}", attempt + 1, ex.Message);
            }
            catch (IOException ex) when (attempt < 2)
            {
                Trace.TraceWarning("Open-Meteo response interrupted: {0}", ex.Message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= 2)
                    throw new TimeoutException("Open-Meteo did not respond in time.");
                Trace.TraceWarning("Open-Meteo attempt {0} timed out.", attempt + 1);
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransient(HttpStatusCode? status) => status is null or HttpStatusCode.RequestTimeout
        or HttpStatusCode.TooManyRequests || (int?)status >= 500;
}
