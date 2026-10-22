using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;

namespace GMTFV.Wpf.Services;

// Each retry starts a fresh file; no Range resume without a validated server entity.
public sealed class ToolTransfer(HttpClient http, TimeSpan? headerTimeout = null, TimeSpan? idleTimeout = null, TimeSpan? totalTimeout = null)
{
    public async Task DownloadAsync(string url, Stream output, long limit, IProgress<ToolSetupProgress>? progress, string tool, CancellationToken token)
    {
        using var total = CancellationTokenSource.CreateLinkedTokenSource(token);
        total.CancelAfter(totalTimeout ?? TimeSpan.FromMinutes(15));
        try {
            for (int attempt = 1; ; attempt++) {
                TimeSpan? retryAfter = null;
                try {
                    output.SetLength(0); output.Position = 0;
                    using var headers = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
                    headers.CancelAfter(headerTimeout ?? TimeSpan.FromSeconds(30));
                    HttpResponseMessage response;
                    try { response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, headers.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (!total.IsCancellationRequested) { throw new ToolTransferTimeoutException(ToolSetupError.ConnectTimeout, "서버 연결·응답 대기 시간이 초과되었습니다."); }
                    headers.CancelAfter(Timeout.InfiniteTimeSpan);
                    using (response) {
                        retryAfter = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow);
                        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) && remaining.Contains("0") && response.StatusCode == HttpStatusCode.Forbidden) {
                            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetTime) && long.TryParse(resetTime.FirstOrDefault(), out long seconds) && seconds is >= 0 and <= 253402300799)
                                retryAfter = DateTimeOffset.FromUnixTimeSeconds(seconds) - DateTimeOffset.UtcNow;
                            throw new ToolRateLimitException(response.StatusCode, "GitHub API 요청 제한(HTTP 403). X-RateLimit-Reset=" + (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset) ? string.Join(",", reset) : "unknown") + (retryAfter.HasValue ? " · " + Math.Ceiling(retryAfter.Value.TotalSeconds) + "초 후 재시도 가능" : ""));
                        }
                        if (!response.IsSuccessStatusCode && retryAfter > TimeSpan.FromSeconds(60)) throw new HttpRequestException("HTTP " + (int)response.StatusCode + " · Retry-After=" + Math.Ceiling(retryAfter.Value.TotalSeconds) + "초. 자동 재시도 대기 한도를 넘었습니다.", null, response.StatusCode);
                        response.EnsureSuccessStatusCode();
                        long? size = response.Content.Headers.ContentLength;
                        if (size > limit) throw new InvalidDataException("배포 파일이 허용 크기를 넘었습니다.");
                        await using var input = await response.Content.ReadAsStreamAsync(total.Token).ConfigureAwait(false);
                        var clock = Stopwatch.StartNew();
                        TimeSpan last = TimeSpan.FromSeconds(-1);
                        long received = 0;
                        var buffer = new byte[81920];
                        progress?.Report(new(tool, 3, "다운로드 중", 0, size));
                        using var idle = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
                        while (true) {
                            idle.CancelAfter(idleTimeout ?? TimeSpan.FromSeconds(30));
                            int length;
                            try { length = await input.ReadAsync(buffer, idle.Token).ConfigureAwait(false); }
                            catch (OperationCanceledException) when (!total.IsCancellationRequested) { throw new ToolTransferTimeoutException(ToolSetupError.IdleTimeout, "장시간 데이터를 받지 못해 전송을 중단했습니다."); }
                            idle.CancelAfter(Timeout.InfiniteTimeSpan);
                            if (length == 0) break;
                            received += length;
                            if (received > limit) throw new InvalidDataException("배포 응답이 허용 크기를 넘었습니다.");
                            await output.WriteAsync(buffer.AsMemory(0, length), total.Token).ConfigureAwait(false);
                            if (clock.Elapsed - last >= TimeSpan.FromMilliseconds(200)) {
                                last = clock.Elapsed; progress?.Report(new(tool, 3, "다운로드 중", received, size, received / Math.Max(.001, clock.Elapsed.TotalSeconds)));
                            }
                        }
                        if (size.HasValue && size != received) throw new HttpRequestException("전송 중 연결이 끊겼습니다. Content-Length와 수신 크기가 다릅니다.");
                        progress?.Report(new(tool, 3, "다운로드 중", received, size, received / Math.Max(.001, clock.Elapsed.TotalSeconds)));
                        return;
                    }
                }
                catch (Exception ex) when (attempt < 3 && !total.IsCancellationRequested && ToolSetupDiagnostics.Classify(ex).Retryable && retryAfter.GetValueOrDefault() <= TimeSpan.FromSeconds(60)) {
                    TimeSpan delay = retryAfter.HasValue ? TimeSpan.FromTicks(Math.Max(0, retryAfter.Value.Ticks)) : TimeSpan.FromMilliseconds(Math.Pow(2, attempt - 1) * 1000 + Random.Shared.Next(100, 501));
                    progress?.Report(new(tool, 3, $"{ToolSetupDiagnostics.Classify(ex).Message} · {delay.TotalSeconds:F1}초 후 재시도 {attempt + 1}/3"));
                    await Task.Delay(delay, total.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new ToolTransferTimeoutException(ToolSetupError.DownloadTimeout, "전체 도구 다운로드 제한 시간이 초과되었습니다."); }
    }
}
