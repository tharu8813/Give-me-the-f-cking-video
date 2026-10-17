using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.DownloadEngine {
    public sealed class YtDlpClient {
        private readonly string directory;
        private readonly IProcessRunner runner;
        private readonly DiagnosticLog log;
        public YtDlpClient(string directory, IProcessRunner runner = null, DiagnosticLog log = null) {
            this.directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
            this.runner = runner ?? new ProcessRunner();
            this.log = log ?? new DiagnosticLog();
        }

        public static List<string> CommonArguments(string directory) {
            var args = new List<string> { "--ignore-config", "--no-playlist", "--encoding", "utf-8", "--socket-timeout", "20",
                "--retries", "2", "--fragment-retries", "2", "--retry-sleep", "http:exp=1:8" };
            string deno = ToolDiagnostics.ResolveDeno(directory);
            if (deno != null) { args.Add("--js-runtimes"); args.Add("deno:" + deno); }
            string ffmpeg = Path.Combine(directory, "ffmpeg.exe");
            if (File.Exists(ffmpeg)) { args.Add("--ffmpeg-location"); args.Add(directory); }
            return args;
        }

        public async Task<ProcessResult> RunAsync(IEnumerable<string> arguments, TimeSpan timeout, CancellationToken token,
            string jobId = null, Action<string> output = null, Action<string> error = null) {
            string id = jobId ?? Guid.NewGuid().ToString("N");
            var args = CommonArguments(directory);
            args.AddRange(arguments);
            log.Write(id, "stage=yt-dlp start");
            try {
                await ToolDiagnostics.CheckDenoAsync(directory, runner, token).ConfigureAwait(false);
                ProcessResult result = await runner.RunAsync(Path.Combine(directory, "yt-dlp.exe"), CommandLine.Join(args), timeout, token, output, error).ConfigureAwait(false);
                log.Write(id, "stage=yt-dlp exit=" + result.ExitCode + " stderr=" + result.Error);
                result.EnsureSuccess();
                return result;
            } catch (Exception ex) {
                log.Write(id, "stage=yt-dlp failure=" + RecoveryPolicy.Classify(ex) + " " + ((ex as ToolExecutionException)?.Error ?? ex.Message));
                throw;
            }
        }
    }
}
