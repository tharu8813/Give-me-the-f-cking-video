using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.DownloadEngine;
using GMTFV.services;
using Newtonsoft.Json.Linq;

namespace GMTFV.tools {
    internal static class DenoTool {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        public static async Task EnsureAsync(string directory, CancellationToken token) {
            await Gate.WaitAsync(token);
            string work = null;
            try {
                string installed = ToolDiagnostics.ResolveDeno(directory);
                if (installed != null) {
                    await ToolDiagnostics.CheckDenoAsync(directory, new ProcessRunner(), token);
                    return;
                }
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                    deadline.CancelAfter(TimeSpan.FromMinutes(2));
                    using (var response = await Tol.SharedHttpClient.GetAsync("https://api.github.com/repos/denoland/deno/releases/latest", deadline.Token)) {
                        response.EnsureSuccessStatusCode();
                        var release = JObject.Parse(await response.Content.ReadAsStringAsync());
                        const string name = "deno-x86_64-pc-windows-msvc.zip";
                        var asset = (release["assets"] as JArray)?.FirstOrDefault(item => item["name"]?.ToString() == name);
                        if (asset == null) throw new InvalidOperationException("Deno Windows 배포 파일을 찾을 수 없습니다.");
                        string digest = asset["digest"]?.ToString();
                        if (string.IsNullOrWhiteSpace(digest)) throw new InvalidOperationException("Deno 배포 파일의 SHA-256 정보가 없습니다.");
                        work = Path.Combine(directory, ".deno-" + Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(work);
                        string zip = Path.Combine(work, "deno.zip");
                        using (var download = await Tol.SharedHttpClient.GetAsync(asset["browser_download_url"].ToString(), deadline.Token)) {
                            download.EnsureSuccessStatusCode();
                            using (var input = await download.Content.ReadAsStreamAsync())
                            using (var output = File.Create(zip)) await input.CopyToAsync(output, 81920, deadline.Token);
                        }
                        FileIntegrityVerifier.VerifySha256(zip, digest);
                        using (var archive = ZipFile.OpenRead(zip)) {
                            var executable = archive.GetEntry("deno.exe");
                            if (executable == null) throw new InvalidDataException("Deno 실행 파일이 없습니다.");
                            executable.ExtractToFile(Path.Combine(work, "deno.exe"));
                        }
                        await ToolDiagnostics.CheckDenoAsync(work, new ProcessRunner(), token);
                        FileIntegrityVerifier.InstallVerifiedFile(Path.Combine(work, "deno.exe"), Path.Combine(directory, "deno.exe"));
                    }
                }
            } finally {
                if (work != null) {
                    try { Directory.Delete(work, true); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                Gate.Release();
            }
        }
    }
}
