using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using GMTFV.DownloadEngine;
namespace GMTFV.Wpf.Services;

// Installs a new private bundle. Shared or running tool files are never replaced.
public sealed class ToolSetupService(string root, HttpClient? client = null, IProcessRunner? runner = null)
{
    private static readonly HttpClient Http = CreateClient();
    private readonly HttpClient http = client ?? Http;
    private readonly IProcessRunner processes = runner ?? new ProcessRunner();
    private static HttpClient CreateClient() {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GMTFV/1.0"); return client;
    }
    public async Task<string> InstallAsync(IProgress<string>? progress, CancellationToken token)
    {
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("도구 저장 경로는 전체 경로여야 합니다.");
        string id = Guid.NewGuid().ToString("N"), work = Path.Combine(root, ".prepare-" + id), bundle = Path.Combine(root, "bundle-" + id);
        Directory.CreateDirectory(work);
        try {
            progress?.Report("1/4 · yt-dlp 배포 정보와 SHA-256 확인");
            await GitHubAssetAsync("yt-dlp/yt-dlp", "yt-dlp.exe", Path.Combine(work, "yt-dlp.exe"), token);
            progress?.Report("2/4 · Deno 다운로드와 검증");
            string denoZip = Path.Combine(work, "deno.zip");
            await GitHubAssetAsync("denoland/deno", "deno-x86_64-pc-windows-msvc.zip", denoZip, token);
            ExtractExecutables(denoZip, work, ["deno.exe"]);
            progress?.Report("3/4 · FFmpeg 및 FFprobe 다운로드와 검증");
            const string ffmpeg = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
            string checksum = await ReadTextAsync(ffmpeg + ".sha256", token);
            var hash = Regex.Match(checksum, @"\A\s*([a-fA-F0-9]{64})(?:\s|$)");
            if (!hash.Success) throw new InvalidDataException("FFmpeg 배포처가 유효한 SHA-256을 제공하지 않았습니다.");
            string ffmpegZip = Path.Combine(work, "ffmpeg.zip");
            await DownloadAsync(ffmpeg, ffmpegZip, 512L * 1024 * 1024, token);
            await VerifyAsync(ffmpegZip, hash.Groups[1].Value, token);
            ExtractExecutables(ffmpegZip, work, ["ffmpeg.exe", "ffprobe.exe"]);
            progress?.Report("4/4 · 실행 확인 후 새 도구 묶음으로 전환");
            foreach (string name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }) {
                var result = await processes.RunAsync(Path.Combine(work, name), name.StartsWith("ff") ? "-version" : "--version", TimeSpan.FromSeconds(15), token);
                result.EnsureSuccess();
                if (string.IsNullOrWhiteSpace(result.Output)) throw new InvalidDataException("도구가 버전 정보를 반환하지 않았습니다: " + name);
            }
            await ToolDiagnostics.CheckDenoAsync(work, processes, token);
            File.Delete(denoZip); File.Delete(ffmpegZip);
            await File.WriteAllTextAsync(Path.Combine(work, "sources.txt"), "yt-dlp: https://github.com/yt-dlp/yt-dlp/releases\nDeno: https://github.com/denoland/deno/releases\nFFmpeg: https://www.gyan.dev/ffmpeg/builds/\nSHA-256 verified before execution.\n", token);
            token.ThrowIfCancellationRequested();
            Directory.Move(work, bundle);
            return bundle;
        }
        finally {
            // work is a newly-created GUID child of root, never a user-selected tool directory.
            if (Directory.Exists(work)) { try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
    }
    private async Task GitHubAssetAsync(string repository, string name, string destination, CancellationToken token)
    {
        using var doc = JsonDocument.Parse(await ReadTextAsync("https://api.github.com/repos/" + repository + "/releases/latest", token));
        var asset = doc.RootElement.GetProperty("assets").EnumerateArray().SingleOrDefault(a => a.GetProperty("name").GetString() == name);
        if (asset.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException(name + " 배포 파일을 찾을 수 없습니다.");
        string digest = asset.TryGetProperty("digest", out var digestValue) ? digestValue.GetString() ?? "" : "";
        if (!Regex.IsMatch(digest, @"\Asha256:[a-fA-F0-9]{64}\z")) throw new InvalidDataException(name + " SHA-256 정보가 없어 설치를 중단했습니다.");
        string url = asset.GetProperty("browser_download_url").GetString() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || !uri.AbsolutePath.StartsWith("/" + repository + "/releases/download/", StringComparison.Ordinal))
            throw new InvalidDataException("공식 저장소의 배포 주소가 아닙니다.");
        await DownloadAsync(url, destination, 256L * 1024 * 1024, token);
        await VerifyAsync(destination, digest[7..], token);
    }
    private async Task<string> ReadTextAsync(string url, CancellationToken token) {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token); response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        await CopyLimitedAsync(stream, buffer, 2 * 1024 * 1024, token);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
    private async Task DownloadAsync(string url, string path, long limit, CancellationToken token) {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token); response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("도구 배포 파일이 허용 크기를 넘었습니다.");
        using var input = await response.Content.ReadAsStreamAsync(token);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await CopyLimitedAsync(input, output, limit, token);
    }
    private static async Task CopyLimitedAsync(Stream input, Stream output, long limit, CancellationToken token) {
        byte[] buffer = new byte[81920]; long total = 0; int length;
        while ((length = await input.ReadAsync(buffer, token)) > 0) {
            total += length; if (total > limit) throw new InvalidDataException("배포 응답이 허용 크기를 넘었습니다.");
            await output.WriteAsync(buffer.AsMemory(0, length), token);
        }
    }
    private static async Task VerifyAsync(string path, string digest, CancellationToken token) {
        await using var stream = File.OpenRead(path);
        string actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
        if (!actual.Equals(digest, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA-256 검증에 실패했습니다. 기존 도구는 그대로 유지됩니다.");
    }
    private static void ExtractExecutables(string zip, string folder, string[] names) {
        using var archive = ZipFile.OpenRead(zip);
        foreach (string name in names) {
            var candidates = archive.Entries.Where(e => e.FullName == name || e.FullName.EndsWith("/bin/" + name, StringComparison.Ordinal)).ToArray();
            if (candidates.Length != 1 || candidates[0].Length > 256L * 1024 * 1024) throw new InvalidDataException("도구 압축 파일의 구성이 올바르지 않습니다: " + name);
            candidates[0].ExtractToFile(Path.Combine(folder, name));
        }
        string notices = Path.Combine(folder, "notices"); Directory.CreateDirectory(notices);
        foreach (var entry in archive.Entries.Where(e => e.Length is > 0 and < 2 * 1024 * 1024 && (e.Name.Contains("license", StringComparison.OrdinalIgnoreCase) || e.Name.Contains("copying", StringComparison.OrdinalIgnoreCase) || e.Name.StartsWith("readme", StringComparison.OrdinalIgnoreCase))).Take(32)) {
            string name = GMTFV.Core.WindowsFileName.Sanitize(Path.GetFileNameWithoutExtension(zip) + "-" + entry.Name);
            string path = Path.Combine(notices, name);
            if (!File.Exists(path)) entry.ExtractToFile(path);
        }
    }
}
