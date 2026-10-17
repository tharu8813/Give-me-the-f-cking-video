using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.DownloadEngine {
    public static class ToolDiagnostics {
        private static readonly ConcurrentDictionary<string, string> Versions = new ConcurrentDictionary<string, string>();
        public static async Task<string> VersionAsync(string path, IProcessRunner runner, CancellationToken token) {
            if (!File.Exists(path)) return "missing";
            string key = Path.GetFullPath(path) + ":" + File.GetLastWriteTimeUtc(path).Ticks;
            if (Versions.TryGetValue(key, out string cached)) return cached;
            try {
                var result = await runner.RunAsync(path, Path.GetFileName(path).Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ? "-version" : "--version",
                    TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                result.EnsureSuccess();
                string version = (result.Output ?? "").Split('\r', '\n')[0];
                if (!string.IsNullOrWhiteSpace(version)) Versions[key] = version;
                return version;
            } catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is ToolExecutionException || ex is TimeoutException || ex is System.ComponentModel.Win32Exception) { return "unavailable: " + ex.Message; }
        }

        public static string ResolveDeno(string directory) {
            string local = Path.Combine(directory, "deno.exe");
            if (File.Exists(local)) return local;
            foreach (string part in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)) {
                try { string path = Path.Combine(part.Trim('"'), "deno.exe"); if (Path.IsPathRooted(path) && File.Exists(path)) return path; }
                catch (ArgumentException) { }
            }
            return null;
        }

        public static async Task CheckDenoAsync(string directory, IProcessRunner runner, CancellationToken token) {
            string deno = ResolveDeno(directory);
            if (deno == null) return; // Some videos work without JS; yt-dlp reports when it is required.
            string version = await VersionAsync(deno, runner, token).ConfigureAwait(false);
            Match match = Regex.Match(version, @"deno\s+(\d+\.\d+\.\d+)");
            if (!match.Success || !Version.TryParse(match.Groups[1].Value, out Version parsed) || parsed < new Version(2, 3, 0))
                throw new InvalidOperationException("Deno 2.3.0 이상이 필요합니다. yt-dlp와 Deno를 함께 업데이트하세요.");
        }
    }
}
