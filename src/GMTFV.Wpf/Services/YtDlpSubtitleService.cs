using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace GMTFV.Wpf.Services;

public sealed class YtDlpSubtitleService {
    public async Task<IReadOnlyList<SubtitleOption>> GetAvailableAsync(string sourceUrl, CancellationToken cancellationToken) {
        string output = await RunAsync(new[] { "--ignore-config", "--no-plugin-dirs", "--quiet", "--no-warnings", "--extractor-args", "youtube:player_client=tv,web_embedded,android_vr", "--no-playlist", "--skip-download", "--dump-single-json", sourceUrl }, cancellationToken);
        using JsonDocument document = ParseJsonOutput(output);

        var result = new List<SubtitleOption>();
        AddOptions(document.RootElement, "subtitles", isAutomatic: false, result);
        AddOptions(document.RootElement, "automatic_captions", isAutomatic: true, result);
        return result.OrderBy(item => item.LanguageCode).ThenBy(item => item.IsAutomatic).ToArray();
    }

    public async Task DownloadAsync(string sourceUrl, SubtitleOption subtitle, string outputTemplate, CancellationToken cancellationToken) {
        string writeOption = subtitle.IsAutomatic ? "--write-auto-subs" : "--write-subs";
        await RunAsync(new[] { "--ignore-config", "--no-plugin-dirs", "--extractor-args", "youtube:player_client=tv,web_embedded,android_vr", "--no-playlist", "--skip-download", writeOption, "--sub-langs", subtitle.LanguageCode, "--sub-format", "srt/best", "--convert-subs", "srt", "-o", outputTemplate, sourceUrl }, cancellationToken);
    }

    private static JsonDocument ParseJsonOutput(string output) {
        if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("yt-dlp에서 자막 정보를 받지 못했습니다.");
        int start = output.IndexOf('{');
        int end = output.LastIndexOf('}');
        if (start < 0 || end < start) throw new InvalidOperationException("yt-dlp의 자막 정보가 JSON 형식이 아닙니다.");
        try { return JsonDocument.Parse(output[start..(end + 1)]); }
        catch (JsonException ex) { throw new InvalidOperationException("yt-dlp 자막 정보를 해석하지 못했습니다.", ex); }
    }

    private static void AddOptions(JsonElement root, string propertyName, bool isAutomatic, ICollection<SubtitleOption> result) {
        if (!root.TryGetProperty(propertyName, out JsonElement subtitles) || subtitles.ValueKind != JsonValueKind.Object) return;
        foreach (JsonProperty language in subtitles.EnumerateObject()) {
            string extension = SelectPreferredExtension(language.Value);
            result.Add(new SubtitleOption(language.Name, extension, isAutomatic));
        }
    }

    private static string SelectPreferredExtension(JsonElement formats) {
        if (formats.ValueKind != JsonValueKind.Array) return "vtt";
        string[] preference = { "srt", "vtt", "srv3", "ttml", "json3" };
        var extensions = formats.EnumerateArray()
            .Select(format => format.TryGetProperty("ext", out JsonElement ext) ? ext.GetString() : null)
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Cast<string>()
            .ToArray();
        return preference.FirstOrDefault(preferred => extensions.Contains(preferred, StringComparer.OrdinalIgnoreCase)) ?? extensions.FirstOrDefault() ?? "vtt";
    }

    private static async Task<string> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken) {
        string path = Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");
        if (!File.Exists(path)) throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다.", path);
        var info = new ProcessStartInfo { FileName = path, WorkingDirectory = AppContext.BaseDirectory, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        process.Start();
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(30));
        CancellationToken operationToken = timeoutCancellation.Token;
        using CancellationTokenRegistration registration = operationToken.Register(() => { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { } });
        Task<string> output = process.StandardOutput.ReadToEndAsync(operationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(operationToken);
        try {
            await Task.WhenAll(process.WaitForExitAsync(operationToken), output, error);
        } catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            throw new TimeoutException("자막 정보 조회 또는 저장이 30초 안에 끝나지 않았습니다. 네트워크 연결을 확인한 뒤 다시 시도해주세요.");
        }
        if (process.ExitCode != 0) {
            string detail = (await error).Trim();
            if (detail.Length > 1_500) detail = detail[..1_500] + "\n(오류 내용 일부만 표시했습니다.)";
            throw new InvalidOperationException("자막 처리 실패\n" + detail);
        }
        return await output;
    }
}

public sealed record SubtitleOption(string LanguageCode, string Extension, bool IsAutomatic) {
    public string DisplayName => $"{LanguageCode} · {(IsAutomatic ? "자동 생성" : "제공 자막")} · {Extension}";
}
