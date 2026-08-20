using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace GMTFV.Wpf.Services;

/// <summary>
/// WPF 앱에서 yt-dlp를 실행해 단일 영상의 메타데이터를 읽습니다.
/// UI, 메시지 상자, 파일 저장과 분리되어 있어 이후 플랫폼 어댑터로 교체할 수 있습니다.
/// </summary>
public sealed class YtDlpMetadataService {
    public async Task<VideoMetadataResult> GetAsync(string normalizedUrl, CancellationToken cancellationToken) {
        string toolPath = Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");
        if (!File.Exists(toolPath)) {
            throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다. 설치 파일을 다시 실행해 복구해주세요.", toolPath);
        }

        var startInfo = new ProcessStartInfo {
            FileName = toolPath,
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        startInfo.ArgumentList.Add("--ignore-config");
        startInfo.ArgumentList.Add("--no-plugin-dirs");
        // 플러그인/PO Token 공급자를 사용할 수 없는 정보 조회도 최대한 안정적으로 처리합니다.
        startInfo.ArgumentList.Add("--extractor-args");
        startInfo.ArgumentList.Add("youtube:player_client=tv,web_embedded,android_vr");
        startInfo.ArgumentList.Add("--no-playlist");
        startInfo.ArgumentList.Add("--dump-single-json");
        startInfo.ArgumentList.Add("--skip-download");
        startInfo.ArgumentList.Add(normalizedUrl);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start()) throw new InvalidOperationException("yt-dlp 프로세스를 시작하지 못했습니다.");

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(30));
        CancellationToken operationToken = timeoutCancellation.Token;
        using var cancellationRegistration = operationToken.Register(() => TryTerminate(process));
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(operationToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(operationToken);
        try {
            await Task.WhenAll(process.WaitForExitAsync(operationToken), outputTask, errorTask);
        } catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            throw new TimeoutException("영상 정보 조회가 30초 안에 끝나지 않았습니다. 네트워크 연결이나 YouTube 접근 상태를 확인한 뒤 다시 시도해주세요.");
        }

        string output = await outputTask;
        string error = await errorTask;
        if (process.ExitCode != 0) throw CreateMetadataException(error, process.ExitCode);
        if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("yt-dlp에서 영상 정보를 받지 못했습니다.");

        return Parse(output, normalizedUrl);
    }

    private static VideoMetadataResult Parse(string json, string normalizedUrl) {
        try {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string videoId = GetString(root, "id") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(videoId)) throw new InvalidOperationException("영상 ID를 읽지 못했습니다.");

            var qualities = new List<VideoQualityOption>();
            if (root.TryGetProperty("formats", out JsonElement formats) && formats.ValueKind == JsonValueKind.Array) {
                foreach (JsonElement format in formats.EnumerateArray()) {
                    if (!TryGetInt(format, "height", out int height) || height <= 0) continue;
                    string videoCodec = GetString(format, "vcodec") ?? string.Empty;
                    string protocol = GetString(format, "protocol") ?? string.Empty;
                    if (videoCodec.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                        videoCodec.Equals("images", StringComparison.OrdinalIgnoreCase) ||
                        protocol.Contains("mhtml", StringComparison.OrdinalIgnoreCase)) continue;
                    int fps = TryGetInt(format, "fps", out int parsedFps) ? parsedFps : 0;
                    if (!qualities.Any(item => item.Height == height && item.FramesPerSecond == fps)) {
                        qualities.Add(new VideoQualityOption(height, fps));
                    }
                }
            }

            return new VideoMetadataResult(
                normalizedUrl,
                videoId,
                GetString(root, "title") ?? "제목 없음",
                GetString(root, "uploader") ?? GetString(root, "channel") ?? "알 수 없음",
                GetDuration(root),
                "https://i.ytimg.com/vi/" + videoId + "/mqdefault.jpg",
                qualities.OrderByDescending(item => item.Height).ThenByDescending(item => item.FramesPerSecond).ToArray());
        } catch (JsonException ex) {
            throw new InvalidOperationException("yt-dlp 메타데이터 JSON을 해석하지 못했습니다.", ex);
        }
    }

    private static Exception CreateMetadataException(string error, int exitCode) {
        string message = error ?? string.Empty;
        if (message.Contains("Private video", StringComparison.OrdinalIgnoreCase)) return new InvalidOperationException("비공개 영상입니다.");
        if (message.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase)) return new InvalidOperationException("영상을 사용할 수 없습니다. 삭제되었거나 공개되지 않은 영상일 수 있습니다.");
        if (message.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase)) return new InvalidOperationException("YouTube에서 접근을 거부했습니다(HTTP 403). 도구 및 PO Token 공급자 상태를 확인해주세요.");
        string detail = message.Trim();
        if (detail.Length > 1_500) detail = detail[..1_500] + "\n(오류 내용 일부만 표시했습니다.)";
        return new InvalidOperationException($"영상 정보 가져오기 실패 (yt-dlp 종료 코드: {exitCode})\n{detail}");
    }

    private static void TryTerminate(Process process) {
        try {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool TryGetInt(JsonElement element, string propertyName, out int value) {
        value = 0;
        if (!element.TryGetProperty(propertyName, out JsonElement property)) return false;
        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value);
    }

    private static TimeSpan GetDuration(JsonElement root) {
        if (!root.TryGetProperty("duration", out JsonElement duration) || duration.ValueKind != JsonValueKind.Number || !duration.TryGetDouble(out double seconds)) return TimeSpan.Zero;
        return TimeSpan.FromSeconds(Math.Max(0, seconds));
    }
}

public sealed record VideoMetadataResult(
    string SourceUrl,
    string VideoId,
    string Title,
    string ChannelName,
    TimeSpan Duration,
    string ThumbnailUrl,
    IReadOnlyList<VideoQualityOption> AvailableQualities);

public sealed record VideoQualityOption(int Height, int FramesPerSecond) {
    public string DisplayText => FramesPerSecond > 0 ? $"{Height}p · {FramesPerSecond}fps" : $"{Height}p";
}
