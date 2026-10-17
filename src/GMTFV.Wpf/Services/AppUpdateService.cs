using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GMTFV.Wpf.Services;

public sealed record AppRelease(Version Version, string Tag, Uri DownloadUrl, string Digest, long Size)
{
    public string FileName => $"GMTFV-{Version}-setup.exe";
}

public sealed class AppUpdateService
{
    public const string Repository = "https://github.com/tharu8813/Give-me-the-f-cking-video";
    private const long MaxInstallerSize = 512L * 1024 * 1024;
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient http;
    public AppUpdateService(HttpClient? http = null) => this.http = http ?? SharedHttp;

    public async Task<AppRelease?> CheckAsync(Version current, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/tharu8813/Give-me-the-f-cking-video/releases/latest");
        request.Headers.UserAgent.ParseAdd("GMTFV/" + current);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var json = new MemoryStream();
        await CopyAsync(input, json, 2 * 1024 * 1024, null, timeout.Token).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json.ToArray());
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Regex.IsMatch(tag, @"\Av?\d+\.\d+\.\d+(\.\d+)?\z", RegexOptions.IgnoreCase) || !Version.TryParse(tag.TrimStart('v', 'V'), out var version))
            throw new InvalidDataException("릴리즈 버전 정보를 확인할 수 없습니다.");
        version = new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        if (version <= current) return null;
        string name = $"GMTFV-{version}-setup.exe";
        var assets = root.GetProperty("assets").EnumerateArray().Where(a => a.GetProperty("name").GetString() == name).ToArray();
        if (assets.Length != 1) throw new InvalidDataException("새 버전의 설치 파일이 없습니다. 릴리즈 페이지에서 확인하세요.");
        var asset = assets[0];
        string url = asset.GetProperty("browser_download_url").GetString() ?? "";
        string digest = asset.TryGetProperty("digest", out var hash) ? hash.GetString() ?? "" : "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) throw new InvalidDataException("업데이트 주소가 올바르지 않습니다.");
        var release = new AppRelease(version, tag, uri, digest, asset.GetProperty("size").GetInt64());
        Validate(release);
        return release;
    }

    private static void Validate(AppRelease release)
    {
        string expected = Repository + "/releases/download/" + Uri.EscapeDataString(release.Tag) + "/" + release.FileName;
        if (release.DownloadUrl.AbsoluteUri != expected) throw new InvalidDataException("공식 GMTFV 저장소의 배포 주소가 아닙니다.");
        if (!Regex.IsMatch(release.Digest, @"\Asha256:[a-fA-F0-9]{64}\z")) throw new InvalidDataException("SHA-256 정보가 없어 업데이트를 중단했습니다. 릴리즈 페이지에서 확인하세요.");
        if (release.Size is <= 0 or > MaxInstallerSize) throw new InvalidDataException("설치 파일의 크기가 올바르지 않습니다.");
    }

    public async Task<string> DownloadAsync(AppRelease release, string directory, IProgress<double>? progress, CancellationToken token)
    {
        Validate(release);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(15));
        string staging = Path.Combine(directory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        string path = Path.Combine(staging, release.FileName);
        bool verified = false;
        try {
            using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long size && size != release.Size) throw new InvalidDataException("설치 파일 크기가 배포 정보와 다릅니다.");
            using (var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false))
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await CopyAsync(input, output, release.Size, progress, timeout.Token).ConfigureAwait(false);
            await VerifyAsync(release, path, timeout.Token).ConfigureAwait(false);
            verified = true;
            return path;
        }
        finally {
            if (!verified) { try { Directory.Delete(staging, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
    }

    public static async Task VerifyAsync(AppRelease release, string path, CancellationToken token)
    {
        Validate(release);
        await using var stream = File.OpenRead(path);
        if (stream.Length != release.Size) throw new InvalidDataException("설치 파일이 불완전합니다. 다시 확인하세요.");
        string actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, token).ConfigureAwait(false));
        if (!actual.Equals(release.Digest[7..], StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("설치 파일의 SHA-256 검증에 실패했습니다. 설치를 중단했습니다.");
    }

    private static async Task CopyAsync(Stream input, Stream output, long limit, IProgress<double>? progress, CancellationToken token)
    {
        byte[] buffer = new byte[81920]; long received = 0; int count;
        while ((count = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0) {
            received += count;
            if (received > limit) throw new InvalidDataException("업데이트 응답이 허용 크기를 넘었습니다.");
            await output.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
            progress?.Report(received * 100d / limit);
        }
    }
}
