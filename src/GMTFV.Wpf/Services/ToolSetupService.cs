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
        var client = new HttpClient(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(15) }) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GMTFV/4.0"); return client;
    }
    public async Task<string> InstallAsync(IProgress<string>? progress, CancellationToken token, IProgress<ToolSetupProgress>? details = null)
    {
        token.ThrowIfCancellationRequested();
        if (!Path.IsPathFullyQualified(root)) throw new ArgumentException("도구 저장 경로는 전체 경로여야 합니다.");
        void Report(ToolSetupProgress value) { details?.Report(value); progress?.Report(value.Text); }
        var reports = new InlineProgress(Report);
        Report(new("도구 묶음", 1, "저장 경로·쓰기·이동·디스크 공간 사전 검사"));
        Preflight(root);
        using var setupLock = new FileStream(Path.Combine(root, ".setup.lock"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        string id = Guid.NewGuid().ToString("N"), work = Path.Combine(root, ".prepare-" + id), bundle = Path.Combine(root, "bundle-" + id);
        Directory.CreateDirectory(work);
        try {
            string yt = await GitHubAssetAsync("yt-dlp/yt-dlp", "yt-dlp.exe", reports, token);
            File.Copy(yt, Path.Combine(work, "yt-dlp.exe"));
            string deno = await GitHubAssetAsync("denoland/deno", "deno-x86_64-pc-windows-msvc.zip", reports, token);
            Report(new("Deno", 5, "안전한 압축 해제"));
            ExtractExecutables(deno, work, ["deno.exe"], token);
            Report(new("FFmpeg", 2, "공식 배포 정보와 SHA-256 확인"));
            const string ffmpeg = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
            string checksum = await ReadTextAsync(ffmpeg + ".sha256", reports, "FFmpeg 배포 정보", token);
            var hash = Regex.Match(checksum, @"\A\s*([a-fA-F0-9]{64})(?:\s|$)");
            if (!hash.Success) throw new InvalidDataException("FFmpeg 배포처가 유효한 SHA-256을 제공하지 않았습니다.");
            string ffmpegZip = await CachedDownloadAsync(ffmpeg, hash.Groups[1].Value, "ffmpeg.zip", 512L * 1024 * 1024, reports, token);
            Report(new("FFmpeg / FFprobe", 5, "안전한 압축 해제"));
            ExtractExecutables(ffmpegZip, work, ["ffmpeg.exe", "ffprobe.exe"], token);
            Report(new("도구 묶음", 6, "실행·버전·Deno 호환성 검사"));
            foreach (string name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }) {
                token.ThrowIfCancellationRequested();
                var result = await processes.RunAsync(Path.Combine(work, name), name.StartsWith("ff") ? "-version" : "--version", TimeSpan.FromSeconds(15), token);
                result.EnsureSuccess();
                string version = result.Output.Trim();
                string pattern = name == "yt-dlp.exe" ? @"\A\d{4}\.\d{2}\.\d{2}" : name == "deno.exe" ? @"\Adeno \d+\.\d+\.\d+" : @"\Aff(mpeg|probe) version \S+";
                if (!Regex.IsMatch(version, pattern)) throw new InvalidOperationException("도구가 유효한 버전 정보를 반환하지 않았습니다: " + name);
            }
            await ToolDiagnostics.CheckDenoAsync(work, processes, token);
            await File.WriteAllTextAsync(Path.Combine(work, "sources.txt"), "yt-dlp: https://github.com/yt-dlp/yt-dlp/releases\nDeno: https://github.com/denoland/deno/releases\nFFmpeg: https://www.gyan.dev/ffmpeg/builds/\nSHA-256 verified before execution.\n", token);
            Report(new("도구 묶음", 7, "검증한 새 묶음 활성화 준비"));
            token.ThrowIfCancellationRequested();
            Directory.Move(work, bundle);
            Report(new("도구 묶음", 8, "준비 완료 · 이전 도구와 설정 보존"));
            return bundle;
        }
        finally {
            // Only this invocation's GUID staging directory is removed; verified cache survives retries.
            if (Directory.Exists(work)) { try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
    }
    public static void Preflight(string folder) {
        Directory.CreateDirectory(folder);
        var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(folder))!);
        if (drive.IsReady && drive.AvailableFreeSpace < 1024L * 1024 * 1024) throw new IOException("도구 설치에 최소 1GB의 여유 공간이 필요합니다.", unchecked((int)0x80070070));
        string probe = Path.Combine(folder, ".write-" + Guid.NewGuid().ToString("N"));
        string moved = probe + ".moved";
        try { using (var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.WriteByte(0); File.Move(probe, moved); }
        finally { if (File.Exists(probe)) File.Delete(probe); if (File.Exists(moved)) File.Delete(moved); }
    }
    private async Task<string> GitHubAssetAsync(string repository, string name, IProgress<ToolSetupProgress> progress, CancellationToken token)
    {
        progress.Report(new(name, 2, "공식 배포 정보와 SHA-256 확인"));
        using var doc = JsonDocument.Parse(await ReadTextAsync("https://api.github.com/repos/" + repository + "/releases/latest", progress, name + " 배포 정보", token));
        var asset = doc.RootElement.GetProperty("assets").EnumerateArray().SingleOrDefault(a => a.GetProperty("name").GetString() == name);
        if (asset.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException(name + " 배포 파일을 찾을 수 없습니다.");
        string digest = asset.TryGetProperty("digest", out var digestValue) ? digestValue.GetString() ?? "" : "";
        if (!Regex.IsMatch(digest, @"\Asha256:[a-fA-F0-9]{64}\z")) throw new InvalidDataException(name + " SHA-256 정보가 없어 설치를 중단했습니다.");
        string url = asset.GetProperty("browser_download_url").GetString() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || !uri.AbsolutePath.StartsWith("/" + repository + "/releases/download/", StringComparison.Ordinal))
            throw new InvalidDataException("공식 저장소의 배포 주소가 아닙니다.");
        return await CachedDownloadAsync(url, digest[7..], name, 256L * 1024 * 1024, progress, token);
    }
    private async Task<string> ReadTextAsync(string url, IProgress<ToolSetupProgress> progress, string name, CancellationToken token) {
        using var buffer = new MemoryStream();
        await new ToolTransfer(http).DownloadAsync(url, buffer, 2 * 1024 * 1024, progress, name, token);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
    private async Task<string> CachedDownloadAsync(string url, string digest, string name, long limit, IProgress<ToolSetupProgress> progress, CancellationToken token) {
        string cache = Path.Combine(root, "verified-cache"); Directory.CreateDirectory(cache);
        string path = Path.Combine(cache, digest.ToLowerInvariant() + "-" + name);
        if (File.Exists(path)) {
            progress.Report(new(name, 4, "캐시 SHA-256 재검증"));
            try { await VerifyAsync(path, digest, token); return path; }
            catch (CryptographicException) { File.Delete(path); }
        }
        string partial = Path.Combine(cache, Guid.NewGuid().ToString("N") + ".partial");
        try {
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await new ToolTransfer(http).DownloadAsync(url, output, limit, progress, name, token);
            progress.Report(new(name, 4, "SHA-256 검증"));
            await VerifyAsync(partial, digest, token);
            token.ThrowIfCancellationRequested();
            File.Move(partial, path);
            return path;
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }
    private static async Task VerifyAsync(string path, string digest, CancellationToken token) {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        string actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
        if (!actual.Equals(digest, StringComparison.OrdinalIgnoreCase)) throw new CryptographicException("SHA-256 검증에 실패했습니다. 기존 도구는 그대로 유지됩니다.");
    }
    private static void ExtractExecutables(string zip, string folder, string[] names, CancellationToken token) {
        using var archive = ZipFile.OpenRead(zip);
        foreach (string name in names) {
            token.ThrowIfCancellationRequested();
            var candidates = archive.Entries.Where(e => e.FullName == name || e.FullName.EndsWith("/bin/" + name, StringComparison.Ordinal)).ToArray();
            if (candidates.Length != 1 || candidates[0].Length is <= 0 or > 256L * 1024 * 1024 || candidates[0].FullName.Split('/').Any(s => s is "." or "..")) throw new InvalidDataException("도구 압축 파일의 구성이 올바르지 않습니다: " + name);
            Extract(candidates[0], Path.Combine(folder, name), token);
        }
        string notices = Path.Combine(folder, "notices"); Directory.CreateDirectory(notices);
        foreach (var entry in archive.Entries.Where(e => e.Length is > 0 and < 2 * 1024 * 1024 && (e.Name.Contains("license", StringComparison.OrdinalIgnoreCase) || e.Name.Contains("copying", StringComparison.OrdinalIgnoreCase) || e.Name.StartsWith("readme", StringComparison.OrdinalIgnoreCase))).Take(32)) {
            string name = GMTFV.Core.WindowsFileName.Sanitize((names[0] == "deno.exe" ? "deno" : "ffmpeg") + "-" + entry.Name);
            string path = Path.Combine(notices, name);
            if (!File.Exists(path)) Extract(entry, path, token);
        }
    }
    private static void Extract(ZipArchiveEntry entry, string path, CancellationToken token) {
        using var input = entry.Open(); using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920]; long copied = 0; int count;
        while ((count = input.Read(buffer)) > 0) {
            token.ThrowIfCancellationRequested(); copied += count;
            if (copied > entry.Length) throw new InvalidDataException("압축 해제 크기가 배포 정보와 다릅니다.");
            output.Write(buffer, 0, count);
        }
        if (copied != entry.Length) throw new InvalidDataException("압축 파일이 잘렸습니다.");
    }
    private sealed class InlineProgress(Action<ToolSetupProgress> report) : IProgress<ToolSetupProgress> { public void Report(ToolSetupProgress value) => report(value); }
}
