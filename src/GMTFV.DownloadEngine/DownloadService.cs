using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;
using Newtonsoft.Json;

namespace GMTFV.DownloadEngine {
    public sealed class DownloadService {
        private readonly string directory;
        private readonly IProcessRunner runner;
        private readonly DiagnosticLog log;
        public DownloadService(string toolDirectory, IProcessRunner runner = null, DiagnosticLog log = null) {
            directory = Path.GetFullPath(toolDirectory ?? throw new ArgumentNullException(nameof(toolDirectory)));
            this.runner = runner ?? new ProcessRunner(); this.log = log ?? new DiagnosticLog();
        }

        public async Task<string> DownloadAsync(DownloadRequest request, IProgress<DownloadProgress> progress, CancellationToken token) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            token.ThrowIfCancellationRequested();
            string id = Guid.NewGuid().ToString("N");
            string target = WindowsFileName.FitPath(request.OutputPath);
            string work = Path.Combine(Path.GetDirectoryName(target), ".gmtfv-" + id.Substring(0, 12));
            string template = Path.Combine(work, "media").Replace("%", "%%") + ".%(ext)s";
            if (!string.Equals(Path.GetExtension(target), "." + request.Extension, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("출력 파일 확장자와 요청 형식이 다릅니다.");
            // Validate before creating files or starting tools.
            YtDlpDownloadCommandFactory.Create(request, template);
            bool fallback = false, cpu = false, disablePlugins = false;
            try {
                foreach (string tool in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe" }) {
                    string path = Path.Combine(directory, tool);
                    if (!File.Exists(path)) throw new FileNotFoundException("필요한 도구가 없습니다: " + tool, path);
                }
                Directory.CreateDirectory(work);
                Report(progress, DownloadState.Ready, null, "다운로드 준비 중");
                log.Write(id, "app=" + System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version + " stage=prepare");
                foreach (string tool in new[] { "yt-dlp.exe", "ffmpeg.exe" })
                    log.Write(id, tool + " version=" + await ToolDiagnostics.VersionAsync(Path.Combine(directory, tool), runner, token).ConfigureAwait(false));
                string deno = ToolDiagnostics.ResolveDeno(directory);
                log.Write(id, "deno version=" + (deno == null ? "missing; JS may be required" : await ToolDiagnostics.VersionAsync(deno, runner, token).ConfigureAwait(false)));
                var client = new YtDlpClient(directory, runner, log);
                for (int attempt = 1; ; attempt++) {
                    string completedPath = null;
                    DownloadState state = DownloadState.Downloading;
                    DownloadStage stage = DownloadStage.None;
                    string stream = null;
                    DateTime lastReport = DateTime.MinValue;
                    Report(progress, state, null, "다운로드 중 (시도 " + attempt + ")");
                    log.Write(id, "attempt=" + attempt + " strategy=default fallback=" + fallback + " cpu=" + cpu + " pluginsDisabled=" + disablePlugins);
                    try {
                        var progressSync = new object();
                        Action<string> onLine = line => {
                            lock (progressSync) {
                                if (line.StartsWith(YtDlpDownloadCommandFactory.CompletedPrefix, StringComparison.Ordinal)) {
                                    try { completedPath = JsonConvert.DeserializeObject<string>(line.Substring(YtDlpDownloadCommandFactory.CompletedPrefix.Length)); }
                                    catch (JsonException) { completedPath = null; }
                                }
                                DownloadProgress parsed = ParseProgress(line);
                                if (parsed == null) return;
                                DateTime now = DateTime.UtcNow;
                                if (parsed.State != state || parsed.Stage != stage || parsed.StreamId != stream || (now - lastReport).TotalMilliseconds >= 200) {
                                    if (parsed.State != state) log.Write(id, "stage=" + parsed.State);
                                    state = parsed.State; stage = parsed.Stage; stream = parsed.StreamId; lastReport = now; progress?.Report(parsed);
                                }
                            }
                        };
                        await client.RunAsync(YtDlpDownloadCommandFactory.Create(request, template, fallback, cpu, disablePlugins), TimeSpan.FromHours(6), token, id,
                            onLine, onLine).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        CompletedFile.Validate(completedPath, work, request.IsVideo ? request.Extension : "original");
                        string downloadedSource = completedPath;
                        if (!request.IsVideo) {
                            Report(progress, DownloadState.Converting, null, "오디오 스트림 및 처리 방식 확인 중", DownloadStage.ExtractAudio);
                            completedPath = await AudioProcessor.ProcessAsync(completedPath, request, directory, runner, progress, token).ConfigureAwait(false);
                        }
                        Report(progress, DownloadState.Validating, null, "완료 파일 검증 중");
                        log.Write(id, "stage=validating");
                        CompletedFile.Validate(completedPath, work, request.Extension);
                        var media = await MediaProbe.ReadAsync(directory, completedPath, runner, token).ConfigureAwait(false);
                        string actualExtension = Path.GetExtension(completedPath).TrimStart('.').ToLowerInvariant();
                        if (!media.MatchesContainer(actualExtension) || (request.IsVideo ? !media.HasVideo :
                            string.IsNullOrEmpty(media.AudioCodec) || media.HasVideo || (request.Extension != "original" && !AudioProcessor.CompatibleCodec(media.AudioCodec, request.Extension))))
                            throw new InvalidDataException("완료 파일의 실제 코덱·컨테이너·스트림이 요청한 형식과 다릅니다.");
                        token.ThrowIfCancellationRequested();
                        Report(progress, DownloadState.Validating, null, "결과 파일 저장 중", DownloadStage.Save);
                        string published = CompletedFile.Publish(completedPath, request.Extension == "original" ? Path.ChangeExtension(target, actualExtension) : target);
                        log.Write(id, "stage=completed exit=0");
                        Report(progress, DownloadState.Completed, 100, "완료: " + Path.GetFileName(published));
                        try {
                            if (downloadedSource != completedPath && File.Exists(downloadedSource)) File.Delete(downloadedSource);
                            if (Directory.GetFileSystemEntries(work).Length == 0) Directory.Delete(work);
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                        return published;
                    } catch (Exception ex) when (!(ex is OperationCanceledException)) {
                        DownloadError error = RecoveryPolicy.Classify(ex);
                        bool gpu = request.IsVideo && (request.Extension == "avi" || request.Extension == "flv") && request.VideoEncoder != "libx264" && !cpu;
                        log.Write(id, "attempt=" + attempt + " error=" + error + " exit=" + (ex as ToolExecutionException)?.ExitCode);
                        if (!RecoveryPolicy.ShouldRetry(error, attempt, request.AllowQualityFallback, gpu)) throw;
                        if (error == DownloadError.Format) fallback = true;
                        if (error == DownloadError.FFmpeg) {
                            cpu = true;
                            // FFmpeg can leave an incomplete conversion destination; keep its source for CPU recovery.
                            string incomplete = Path.Combine(work, "media." + request.Extension);
                            if (File.Exists(incomplete)) File.Delete(incomplete);
                        }
                        if (error == DownloadError.PoToken) disablePlugins = true;
                        Report(progress, DownloadState.Retrying, null, "자동 복구 대기 중 (다음 시도 " + (attempt + 1) + ")");
                        await Task.Delay(RecoveryPolicy.Delay(attempt), token).ConfigureAwait(false);
                    }
                }
            } catch (OperationCanceledException) {
                log.Write(id, "stage=cancelled; partial files retained in job directory");
                Report(progress, DownloadState.Cancelled, null, "취소됨 · 임시 파일 보존"); throw;
            } catch (Exception ex) {
                DownloadError error = RecoveryPolicy.Classify(ex);
                log.Write(id, "stage=failed error=" + error + " " + ((ex as ToolExecutionException)?.Error ?? ex.Message));
                string message = RecoveryPolicy.Message(error) + "\n상세 로그: " + log.DirectoryPath;
                Report(progress, DownloadState.Failed, null, message);
                throw new InvalidOperationException(message, ex);
            }
        }

        public static DownloadProgress ParseProgress(string line) {
            if (string.IsNullOrEmpty(line)) return null;
            if (line.StartsWith("GMTFV_POSTPROCESS:", StringComparison.Ordinal)) {
                string[] post = line.Substring("GMTFV_POSTPROCESS:".Length).Split('|');
                if (post.Length < 2 || (post[1] != "started" && post[1] != "processing")) return null;
                return Postprocess(post[0]);
            }
            if (line.StartsWith("GMTFV_PROGRESS:", StringComparison.Ordinal)) {
                string[] values = line.Substring("GMTFV_PROGRESS:".Length).Split('|');
                if (values.Length < 4) return null;
                int? percent = null;
                if (double.TryParse(values[1].Trim().TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && !double.IsNaN(number) && !double.IsInfinity(number))
                    percent = (int)Math.Max(0, Math.Min(99, number));
                string speed = values[2].Trim(), eta = values[3].Trim();
                var stage = values.Length >= 6 ? values[4] == "none" ? DownloadStage.AudioDownload : values[5] == "none" ? DownloadStage.VideoDownload : DownloadStage.Download : DownloadStage.Download;
                string label = stage == DownloadStage.AudioDownload ? "오디오 스트림 다운로드" : stage == DownloadStage.VideoDownload ? "비디오 스트림 다운로드" : "다운로드";
                return new DownloadProgress { State = DownloadState.Downloading, Percent = percent,
                    Stage = stage, StreamId = values.Length >= 7 ? values[6] : null,
                    Message = label + (percent.HasValue ? " " + percent + "%" : "") + (speed == "N/A" || speed == "Unknown B/s" ? "" : " · " + speed) + (eta == "N/A" || eta == "Unknown" ? "" : " · 남은 시간 " + eta) };
            }
            var match = Regex.Match(line, @"\[(Merger|ExtractAudio|VideoConvertor|VideoRemuxer|Fixup\w*)\]");
            if (match.Success && !line.Contains("Not ")) return Postprocess(match.Groups[1].Value);
            return null;
        }
        private static DownloadProgress Postprocess(string processor) {
            bool merge = processor.Contains("Merger"), remux = processor.Contains("Remux") || processor.Contains("Fixup"), audio = processor.Contains("ExtractAudio");
            if (processor.Contains("MoveFiles")) return new DownloadProgress { State = DownloadState.Validating, Stage = DownloadStage.Save, Message = "작업 파일 정리 중" };
            if (!merge && !remux && !audio && !processor.Contains("VideoConvertor")) return null;
            return new DownloadProgress { State = merge ? DownloadState.Merging : DownloadState.Converting,
                Stage = merge ? DownloadStage.Merge : remux ? DownloadStage.Remux : audio ? DownloadStage.ExtractAudio : DownloadStage.EncodeVideo,
                Message = merge ? "영상·오디오 병합 중" : remux ? "컨테이너 변경·정리 중 · 재인코딩 없음" : audio ? "오디오 추출 중" : "영상 인코딩 중" };
        }
        private static void Report(IProgress<DownloadProgress> progress, DownloadState state, int? percent, string message, DownloadStage stage = DownloadStage.None) =>
            progress?.Report(new DownloadProgress { State = state, Stage = stage, Percent = percent, Message = message });
    }
}
