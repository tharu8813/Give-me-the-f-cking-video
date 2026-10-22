using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.DownloadEngine {
    public interface IProcessRunner {
        Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken token,
            Action<string> output = null, Action<string> error = null);
    }

    public sealed class ProcessResult {
        public ProcessResult(int exitCode, string output, string error) { ExitCode = exitCode; Output = output; Error = error; }
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }
        public void EnsureSuccess() {
            if (ExitCode != 0) throw new ToolExecutionException(ExitCode, Error);
        }
    }

    public sealed class ToolExecutionException : Exception {
        public ToolExecutionException(int exitCode, string error) : base("외부 도구 실행 실패 (종료 코드 " + exitCode + ")") { ExitCode = exitCode; Error = error; }
        public int ExitCode { get; }
        public string Error { get; }
    }

    public sealed class ProcessRunner : IProcessRunner {
        public async Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken token,
            Action<string> output = null, Action<string> error = null) {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(executable)) throw new FileNotFoundException("필요한 실행 파일이 없습니다.", executable);
            if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
            using (var process = new Process())
            using (var job = new ProcessJob())
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                process.StartInfo = new ProcessStartInfo {
                    FileName = executable, Arguments = arguments ?? string.Empty,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(executable))
                };
                var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.EnableRaisingEvents = true;
                process.Exited += (_, __) => exited.TrySetResult(true);
                Task<string> stdout = null, stderr = null;
                deadline.CancelAfter(timeout);
                try {
                    process.Start();
                    job.Attach(process);
                    stdout = PumpAsync(process.StandardOutput, output, output == null ? 16 * 1024 * 1024 : 64 * 1024, output != null, () => job.Dispose());
                    stderr = PumpAsync(process.StandardError, error, 64 * 1024, true, () => job.Dispose());
                    using (deadline.Token.Register(() => exited.TrySetCanceled())) {
                        await exited.Task.ConfigureAwait(false);
                        // A crashed parent can leave a child holding its pipes open.
                        job.Dispose();
                        await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
                        deadline.Token.ThrowIfCancellationRequested();
                    }
                    return new ProcessResult(process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
                } catch (OperationCanceledException) {
                    if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                    throw new TimeoutException("외부 도구 응답 시간이 초과되었습니다.");
                } finally {
                    job.Dispose();
                    try { if (process.Id > 0 && !process.HasExited) process.Kill(); }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                    // Observe both drains before disposing their streams; no callbacks after return.
                    if (stdout != null && stderr != null) {
                        try { await Task.WhenAll(stdout, stderr).ConfigureAwait(false); }
                        catch (IOException) { }
                        catch (Exception) { /* Both drains are observed; preserve the original failure. */ }
                    }
                }
            }
        }

        private static async Task<string> PumpAsync(StreamReader reader, Action<string> report, int limit, bool keepTail, Action stop) {
            var buffer = new StringBuilder();
            bool overflow = false;
            Exception callbackFailure = null;
            var line = new StringBuilder();
            var chunk = new char[8192];
            int count;
            while ((count = await reader.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(false)) > 0) {
                buffer.Append(chunk, 0, count);
                if (buffer.Length > limit) {
                    overflow = true;
                    if (keepTail) buffer.Remove(0, buffer.Length - limit);
                    else buffer.Length = limit;
                }
                if (report == null || callbackFailure != null) continue;
                for (int i = 0; i < count; i++) {
                    if (chunk[i] == '\n') {
                        try { report(line.ToString().TrimEnd('\r')); } catch (Exception ex) { callbackFailure = ex; stop(); }
                        line.Clear();
                        if (callbackFailure != null) break;
                    } else if (line.Length < limit) line.Append(chunk[i]);
                }
            }
            if (line.Length > 0 && callbackFailure == null) {
                try { report?.Invoke(line.ToString()); } catch (Exception ex) { callbackFailure = ex; stop(); }
            }
            if (overflow && !keepTail) throw new InvalidDataException("외부 도구 출력이 허용 크기를 초과했습니다.");
            if (callbackFailure != null) throw new InvalidOperationException("진행 상태 처리에 실패했습니다.", callbackFailure);
            return buffer.ToString();
        }
    }
}
