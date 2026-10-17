using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;

namespace GMTFV.DownloadEngine {
    public sealed class SubtitleService {
        private readonly YtDlpClient client;
        public SubtitleService(string toolDirectory) : this(toolDirectory, null) { }
        public SubtitleService(string toolDirectory, DiagnosticLog log) { client = new YtDlpClient(toolDirectory, log: log); }
        public async Task<string> GetMetadataAsync(string url, CancellationToken token) {
            var result = await client.RunAsync(new[] { "--dump-single-json", "--skip-download", "--", YouTubeUrl.Normalize(url) }, TimeSpan.FromSeconds(60), token).ConfigureAwait(false);
            return result.Output;
        }

        public async Task<string> DownloadAsync(string url, string language, bool automatic, string target, CancellationToken token) {
            if (!Regex.IsMatch(language ?? "", @"\A[A-Za-z0-9_-]+\z")) throw new ArgumentException("자막 언어가 올바르지 않습니다.");
            target = WindowsFileName.FitPath(target);
            string work = Path.Combine(Path.GetDirectoryName(target), ".gmtfv-sub-" + Guid.NewGuid().ToString("N").Substring(0, 12));
            Directory.CreateDirectory(work);
            await client.RunAsync(new[] { automatic ? "--write-auto-subs" : "--write-subs", "--sub-langs", language, "--skip-download",
                "--sub-format", "srt/best", "--convert-subs", "srt", "-o", Path.Combine(work, "subtitle").Replace("%", "%%") + ".%(ext)s", "--", YouTubeUrl.Normalize(url) }, TimeSpan.FromMinutes(2), token).ConfigureAwait(false);
            string source = Path.Combine(work, "subtitle." + language + ".srt");
            if (!File.Exists(source) || new FileInfo(source).Length == 0) throw new InvalidDataException("새 자막 파일이 생성되지 않았습니다.");
            token.ThrowIfCancellationRequested();
            string result = CompletedFile.Publish(source, target);
            try { if (Directory.GetFileSystemEntries(work).Length == 0) Directory.Delete(work); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return result;
        }
    }
}
