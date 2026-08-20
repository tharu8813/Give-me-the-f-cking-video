using GMTFV.Core;
using GMTFV.Wpf.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GMTFV.Wpf.Services;

/// <summary>yt-dlp/FFmpeg 실행과 출력 진행률을 WPF에 전달하는 플랫폼 어댑터입니다.</summary>
public sealed class YtDlpDownloadService : IDisposable {
    private static readonly Regex DownloadProgressPattern = new(@"\[download\]\s+(?<percent>\d+(?:\.\d+)?)%", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DownloadSpeedPattern = new(@"(?<speed>\d+(?:\.\d+)?\s*[KMGT]?i?B/s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly ConcurrentDictionary<string, Lazy<Task<bool>>> EncoderAvailability = new(StringComparer.OrdinalIgnoreCase);
    private readonly object syncRoot = new();
    private readonly HashSet<Process> activeProcesses = new();
    private readonly PoTokenProviderService poTokenProvider = new();

    public async Task DownloadAsync(DownloadRequest request, IProgress<DownloadProgress> progress, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        string ytdlpPath = RequireTool("yt-dlp.exe");
        string ffmpegPath = RequireTool("ffmpeg.exe");
        Directory.CreateDirectory(request.OutputDirectory);

        bool providerAvailable = await poTokenProvider.EnsureStartedAsync();
        string outputPath = Path.Combine(request.OutputDirectory, BuildFileName(request));
        string videoEncoder = request.Profile.IsVideo
            ? await ResolveVideoEncoderAsync(ffmpegPath, request.Settings.GpuAccelerator, cancellationToken)
            : "libx264";
        if (request.Profile.IsVideo && !string.Equals(request.Settings.GpuAccelerator, "CPU", StringComparison.OrdinalIgnoreCase) && videoEncoder == "libx264") {
            progress.Report(new DownloadProgress(DownloadPhase.Video, 0, $"{request.Settings.GpuAccelerator} 가속을 사용할 수 없어 CPU로 전환합니다…"));
        } else if (request.Profile.IsVideo && videoEncoder != "libx264") {
            progress.Report(new DownloadProgress(DownloadPhase.Video, 0, $"{request.Settings.GpuAccelerator} GPU 가속 확인 · {videoEncoder} 인코더를 사용합니다…"));
        }
        YouTubeDownloadRoute[] routes = providerAvailable
            ? new[] { YouTubeDownloadRoute.MwebWithPoToken, YouTubeDownloadRoute.AndroidVr, YouTubeDownloadRoute.WebSafariHls }
            : new[] { YouTubeDownloadRoute.AndroidVr, YouTubeDownloadRoute.WebSafariHls };
        var failures = new List<string>();
        for (int attemptIndex = 0; attemptIndex < routes.Length; attemptIndex++) {
            YouTubeDownloadRoute route = routes[attemptIndex];
            if (attemptIndex > 0) {
                progress.Report(new DownloadProgress(DownloadPhase.Video, 0, GetRetryMessage(route)));
            }
            DownloadAttemptResult result = await RunAttemptAsync(
                CreateStartInfo(ytdlpPath, ffmpegPath, outputPath, request, route, videoEncoder),
                request,
                route,
                progress,
                cancellationToken);
            if (result.Succeeded) {
                progress.Report(new DownloadProgress(DownloadPhase.Completed, 100, "다운로드 완료"));
                return;
            }

            if (result.TokenProviderFailed) poTokenProvider.ReportFailure();

            failures.Add($"[{GetRouteName(route)}]\n{result.Error.Trim()}");
            bool canRetry = attemptIndex + 1 < routes.Length && IsRetryableFailure(result);
            if (!canRetry) throw CreateDownloadException(result.Error, result.ExitCode);
        }

        throw new InvalidOperationException("모든 YouTube 호환 경로에서 다운로드하지 못했습니다.\n" + string.Join("\n\n", failures));
    }

    private async Task<DownloadAttemptResult> RunAttemptAsync(
        ProcessStartInfo startInfo,
        DownloadRequest request,
        YouTubeDownloadRoute route,
        IProgress<DownloadProgress> progress,
        CancellationToken cancellationToken) {
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start()) throw new InvalidOperationException("yt-dlp 프로세스를 시작하지 못했습니다.");
        Register(process);
        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() => TryTerminate(process));

        var errors = new StringBuilder();
        int lastMergePercent = -1;
        bool mergeStarted = false;
        bool tokenProviderFailed = false;
        DateTime lastProgressTime = DateTime.MinValue;
        try {
            Action<string> handleProgressLine = line => {
                if (route == YouTubeDownloadRoute.MwebWithPoToken && IsPoTokenFailure(line)) {
                    tokenProviderFailed = true;
                    TryTerminate(process);
                    return;
                }
                if (TryParseDownloadProgress(line, out DownloadProgress? parsed) && parsed is not null) {
                    if (ShouldReport(ref lastProgressTime, parsed.Percent)) progress.Report(parsed);
                } else if (line.Contains("Merging formats", StringComparison.OrdinalIgnoreCase) || line.Contains("[Merger]", StringComparison.OrdinalIgnoreCase)) {
                    mergeStarted = true;
                    progress.Report(new DownloadProgress(DownloadPhase.Merging, 90, "영상과 오디오를 병합하는 중…"));
                } else if (line.Contains("[ExtractAudio]", StringComparison.OrdinalIgnoreCase) || line.Contains("Post-process", StringComparison.OrdinalIgnoreCase)) {
                    progress.Report(new DownloadProgress(DownloadPhase.Processing, 98, "출력 형식을 변환하는 중…"));
                } else if (line.Contains("Deleting original file", StringComparison.OrdinalIgnoreCase)) {
                    progress.Report(new DownloadProgress(DownloadPhase.Finishing, 99, "임시 파일을 정리하는 중…"));
                }
            };

            Task stdoutTask = ReadLinesAsync(process.StandardOutput, handleProgressLine, cancellationToken);
            Task stderrTask = ReadLinesAsync(process.StandardError, line => {
                errors.AppendLine(line);
                handleProgressLine(line);
                if (mergeStarted && TryGetMergePercent(line, request.Duration, out int mergePercent) && mergePercent != lastMergePercent) {
                    lastMergePercent = mergePercent;
                    int overallPercent = 90 + mergePercent * 9 / 100;
                    progress.Report(new DownloadProgress(DownloadPhase.Merging, overallPercent, $"영상과 오디오를 병합하는 중… {mergePercent}%"));
                }
            }, cancellationToken);

            await Task.WhenAll(process.WaitForExitAsync(cancellationToken), stdoutTask, stderrTask);
            cancellationToken.ThrowIfCancellationRequested();
            return new DownloadAttemptResult(process.ExitCode == 0 && !tokenProviderFailed, process.ExitCode, errors.ToString(), tokenProviderFailed);
        } finally {
            Unregister(process);
        }
    }

    public void CancelActiveDownloads() {
        Process[] processes;
        lock (syncRoot) processes = activeProcesses.ToArray();
        foreach (Process process in processes) TryTerminate(process);
    }

    private static ProcessStartInfo CreateStartInfo(string ytdlpPath, string ffmpegPath, string outputPath, DownloadRequest request, YouTubeDownloadRoute route, string videoEncoder) {
        var info = new ProcessStartInfo {
            FileName = ytdlpPath,
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        info.ArgumentList.Add("--ignore-config");
        info.ArgumentList.Add("--no-plugin-dirs");
        if (route == YouTubeDownloadRoute.MwebWithPoToken) {
            string pluginDirectory = Path.Combine(AppContext.BaseDirectory, "yt-dlp-plugins");
            info.ArgumentList.Add("--plugin-dirs");
            info.ArgumentList.Add(pluginDirectory);
            info.ArgumentList.Add("--extractor-args");
            info.ArgumentList.Add("youtube:player_client=mweb");
        } else {
            info.ArgumentList.Add("--extractor-args");
            info.ArgumentList.Add(route == YouTubeDownloadRoute.AndroidVr
                ? "youtube:player_client=android_vr"
                : "youtube:player_client=web_safari");
        }
        info.ArgumentList.Add("--ffmpeg-location");
        info.ArgumentList.Add(ffmpegPath);
        info.ArgumentList.Add("--retries"); info.ArgumentList.Add("3");
        info.ArgumentList.Add("--fragment-retries"); info.ArgumentList.Add("3");
        info.ArgumentList.Add("--retry-sleep"); info.ArgumentList.Add("http:exp=1:5");
        info.ArgumentList.Add("--socket-timeout"); info.ArgumentList.Add("15");
        info.ArgumentList.Add("--no-continue");
        DownloadProfile profile = request.Profile;
        if (profile.IsVideo) {
            string format = BuildFormatSelector(profile, route);
            info.ArgumentList.Add("-f"); info.ArgumentList.Add(format);
            info.ArgumentList.Add("--merge-output-format"); info.ArgumentList.Add(profile.Container);
            info.ArgumentList.Add("--postprocessor-args"); info.ArgumentList.Add($"ffmpeg:-progress pipe:2 -nostats -c:v {videoEncoder} -c:a aac -b:a {request.Settings.AudioBitrate}k");
        } else {
            info.ArgumentList.Add("-f"); info.ArgumentList.Add("bestaudio/best");
            info.ArgumentList.Add("--extract-audio"); info.ArgumentList.Add("--audio-format"); info.ArgumentList.Add(profile.Container);
            info.ArgumentList.Add("--postprocessor-args"); info.ArgumentList.Add("ffmpeg:-progress pipe:2 -nostats");
        }
        info.ArgumentList.Add("--no-playlist");
        info.ArgumentList.Add("--newline");
        info.ArgumentList.Add("-o"); info.ArgumentList.Add(outputPath);
        info.ArgumentList.Add(request.SourceUrl);
        return info;
    }

    private static string BuildFormatSelector(DownloadProfile profile, YouTubeDownloadRoute route) {
        bool hasHeight = int.TryParse(profile.Quality?.TrimEnd('p', 'P'), out int height) && height > 0;
        if (route == YouTubeDownloadRoute.WebSafariHls) {
            if (!profile.IsVideo) return "bestaudio[protocol*=m3u8]/best[protocol*=m3u8]";
            return hasHeight
                ? $"best[protocol*=m3u8][height<={height}]/bestvideo[protocol*=m3u8][height<={height}]+bestaudio[protocol*=m3u8]/best[protocol*=m3u8]"
                : "best[protocol*=m3u8]/bestvideo[protocol*=m3u8]+bestaudio[protocol*=m3u8]";
        }

        if (!profile.IsVideo) return "bestaudio/best";
        if (!hasHeight) return route == YouTubeDownloadRoute.MwebWithPoToken ? "bestvideo+bestaudio" : "bestvideo+bestaudio/best";

        string exactVideo = profile.FramesPerSecond > 0
            ? $"bestvideo[height={height}][fps<={profile.FramesPerSecond}]+bestaudio"
            : $"bestvideo[height={height}]+bestaudio";
        string compatibleVideo = $"bestvideo[height<={height}]+bestaudio";
        return route == YouTubeDownloadRoute.MwebWithPoToken
            ? $"{exactVideo}/{compatibleVideo}"
            : $"{exactVideo}/bestvideo[height={height}]+bestaudio/best[height={height}]/{compatibleVideo}/best[height<={height}]";
    }

    private static bool IsPoTokenFailure(string line) =>
        line.Contains("Error fetching PO Token", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("GVS PO Token which was not provided", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("Error while importing module", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("Error reaching GET http://127.0.0.1:4416", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("Error reaching POST /get_pot", StringComparison.OrdinalIgnoreCase);

    private static bool IsRetryableFailure(DownloadAttemptResult result) {
        if (result.TokenProviderFailed) return true;
        string error = result.Error;
        return error.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("Requested format is not available", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("No video formats found", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("not available on this app", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("Sign in to confirm", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRetryMessage(YouTubeDownloadRoute route) => route switch {
        YouTubeDownloadRoute.AndroidVr => "PO Token 경로를 사용할 수 없어 고화질 호환 경로로 다시 시도합니다…",
        YouTubeDownloadRoute.WebSafariHls => "직접 스트림이 거부되어 HLS 호환 경로로 마지막 재시도합니다…",
        _ => "다른 YouTube 다운로드 경로로 다시 시도합니다…"
    };

    private static string GetRouteName(YouTubeDownloadRoute route) => route switch {
        YouTubeDownloadRoute.MwebWithPoToken => "mweb + PO Token",
        YouTubeDownloadRoute.AndroidVr => "Android VR 호환",
        YouTubeDownloadRoute.WebSafariHls => "Web Safari HLS",
        _ => route.ToString()
    };

    private static async Task<string> ResolveVideoEncoderAsync(string ffmpegPath, string accelerator, CancellationToken cancellationToken) {
        string encoder = accelerator switch {
            "NVIDIA" => "h264_nvenc",
            "AMD" => "h264_amf",
            "Intel" => "h264_qsv",
            _ => "libx264"
        };
        if (encoder == "libx264") return encoder;

        string cacheKey = ffmpegPath + "|" + encoder;
        Lazy<Task<bool>> availability = EncoderAvailability.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<bool>>(() => ProbeEncoderAsync(ffmpegPath, encoder), LazyThreadSafetyMode.ExecutionAndPublication));
        return await availability.Value.WaitAsync(cancellationToken) ? encoder : "libx264";
    }

    private static async Task<bool> ProbeEncoderAsync(string ffmpegPath, string encoder) {
        var info = new ProcessStartInfo {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // AMF는 D3D11 장치를 찾더라도 지나치게 작은 64x64 시험 프레임에서 Init 오류를
        // 반환할 수 있습니다. 실제 영상에 가까운 NV12 프레임으로 하드웨어 인코더를 검증합니다.
        foreach (string argument in new[] { "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "color=c=black:s=320x180:r=30:d=0.1", "-frames:v", "2", "-pix_fmt", "nv12", "-c:v", encoder, "-f", "null", "NUL" }) {
            info.ArgumentList.Add(argument);
        }

        try {
            using var process = new Process { StartInfo = info };
            if (!process.Start()) return false;
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(process.WaitForExitAsync(), stdoutTask, stderrTask);
            return process.ExitCode == 0;
        } catch {
            return false;
        }
    }

    private static async Task ReadLinesAsync(StreamReader reader, Action<string> onLine, CancellationToken cancellationToken) {
        while (await reader.ReadLineAsync(cancellationToken) is string line) onLine(line);
    }

    private static bool TryParseDownloadProgress(string line, out DownloadProgress? progress) {
        progress = null;
        Match match = DownloadProgressPattern.Match(line);
        if (!match.Success || !double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) return false;
        int percent = Math.Clamp((int)Math.Round(value), 0, 89);
        Match speedMatch = DownloadSpeedPattern.Match(line);
        string speed = speedMatch.Success ? speedMatch.Groups["speed"].Value.Trim() : string.Empty;
        bool isAudio = line.Contains("audio", StringComparison.OrdinalIgnoreCase) || line.Contains("m4a", StringComparison.OrdinalIgnoreCase) || line.Contains("opus", StringComparison.OrdinalIgnoreCase);
        progress = new DownloadProgress(isAudio ? DownloadPhase.Audio : DownloadPhase.Video, percent, (isAudio ? "오디오 다운로드 중… " : "영상 다운로드 중… ") + percent + "%" + (string.IsNullOrWhiteSpace(speed) ? string.Empty : " · " + speed), speed);
        return true;
    }

    private static bool TryGetMergePercent(string line, TimeSpan duration, out int percent) {
        percent = 0;
        if (duration <= TimeSpan.Zero || !line.StartsWith("out_time_", StringComparison.OrdinalIgnoreCase)) return false;
        string[] parts = line.Split('=', 2);
        if (parts.Length != 2 || !long.TryParse(parts[1], out long rawTime) || rawTime < 0) return false;
        double elapsedSeconds = parts[0].Equals("out_time_us", StringComparison.OrdinalIgnoreCase) || rawTime > duration.TotalMilliseconds * 10 ? rawTime / 1_000_000d : rawTime / 1_000d;
        percent = Math.Clamp((int)Math.Round(elapsedSeconds / duration.TotalSeconds * 100), 0, 100);
        return true;
    }

    private static bool ShouldReport(ref DateTime lastProgressTime, int? percent) {
        DateTime now = DateTime.UtcNow;
        if (percent == 100 || (now - lastProgressTime).TotalMilliseconds >= 160) { lastProgressTime = now; return true; }
        return false;
    }

    private static string BuildFileName(DownloadRequest request) {
        string invalid = new string(Path.GetInvalidFileNameChars());
        string extension = request.Profile.Container;
        string title = (request.Settings.FileNameTemplate ?? "%title%_%date%")
            .Replace("%title%", request.Title ?? "video")
            .Replace("%author%", request.ChannelName ?? "unknown")
            .Replace("%date%", DateTime.Now.ToString("yyyy-MM-dd"))
            .Replace("%id%", request.VideoId)
            .Replace("%num%", request.ListNumber.ToString())
            .Replace("%ext%", extension);
        title = string.Concat(title.Select(character => invalid.Contains(character) ? '_' : character)).Trim();
        if (title.Length > 120) title = title[..120];
        if (string.IsNullOrWhiteSpace(title)) return request.VideoId + "." + extension;
        return title.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase) ? title : title + "." + extension;
    }

    private static string RequireTool(string name) {
        string path = Path.Combine(AppContext.BaseDirectory, name);
        return File.Exists(path) ? path : throw new FileNotFoundException($"{name}를 찾을 수 없습니다. 설치 파일을 다시 실행해 복구해주세요.", path);
    }

    private static Exception CreateDownloadException(string error, int exitCode) {
        if (error.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase)) return new InvalidOperationException("YouTube에서 접근을 거부했습니다(HTTP 403). PO Token 공급자 또는 yt-dlp 버전을 확인해주세요.");
        if (error.Contains("Private video", StringComparison.OrdinalIgnoreCase)) return new InvalidOperationException("비공개 영상은 다운로드할 수 없습니다.");
        return new InvalidOperationException($"yt-dlp 다운로드 실패 (종료 코드: {exitCode})\n{error.Trim()}");
    }

    private void Register(Process process) { lock (syncRoot) activeProcesses.Add(process); }
    private void Unregister(Process process) { lock (syncRoot) activeProcesses.Remove(process); }
    private static void TryTerminate(Process process) { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { } }

    public void Dispose() { CancelActiveDownloads(); poTokenProvider.Dispose(); }
}

public sealed record DownloadRequest(string SourceUrl, string VideoId, string Title, string ChannelName, TimeSpan Duration, string OutputDirectory, int ListNumber, DownloadProfile Profile, DownloadSettings Settings);
internal enum YouTubeDownloadRoute { MwebWithPoToken, AndroidVr, WebSafariHls }
internal sealed record DownloadAttemptResult(bool Succeeded, int ExitCode, string Error, bool TokenProviderFailed);
