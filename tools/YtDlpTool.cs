using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.services;
using static GMTFV.tools.Tol;

namespace GMTFV.tools {
    internal class YtDlpTool {

        // ─────────────────────────────────────────────────
        //  정적 필드
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 애플리케이션 수명 동안 재사용되는 단일 HttpClient 인스턴스.
        /// Timeout·User-Agent는 한 번만 설정됩니다.
        /// </summary>
        private static readonly HttpClient _httpClient = CreateHttpClient();

        /// <summary>
        /// EnsureYtDlpAsync 의 동시 실행을 1개로 제한하는 세마포어.
        /// </summary>
        private static readonly SemaphoreSlim _ensureSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>yt-dlp GitHub 최신 릴리즈 다운로드 URL</summary>
        private const string DownloadUrl =
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        private const string ChecksumUrl =
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS";

        // ─────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────

        /// <summary>
        /// yt-dlp 가 없으면 다운로드하고, 있으면 최신 버전으로 업데이트합니다.
        /// 동시에 여러 호출이 와도 한 번에 하나만 실행됩니다.
        /// </summary>
        public static async Task EnsureYtDlpAsync(
            string baseDir,
            IProgress<FFmpegProgress> progress = null) {

            if (string.IsNullOrWhiteSpace(baseDir))
                throw new ArgumentNullException(nameof(baseDir));

            // 동시 실행 방지
            await _ensureSemaphore.WaitAsync();
            try {
                await EnsureYtDlpCoreAsync(baseDir, progress);
            } finally {
                _ensureSemaphore.Release();
            }
        }

        /// <summary>
        /// yt-dlp 가 설치되어 있는지 확인합니다.
        /// </summary>
        public static bool IsYtDlpInstalled(string baseDir) {
            if (string.IsNullOrWhiteSpace(baseDir))
                return false;

            try {
                return File.Exists(Path.Combine(baseDir, "yt-dlp.exe"));
            } catch (Exception ex) {
                Console.WriteLine($"IsYtDlpInstalled 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// yt-dlp 를 강제로 재다운로드합니다.
        /// 임시 파일에 다운로드한 뒤 성공하면 기존 파일을 교체합니다.
        /// 실패해도 기존 실행 파일은 유지됩니다.
        /// </summary>
        public static async Task ForceUpdateYtDlpAsync(
            string baseDir,
            IProgress<FFmpegProgress> progress = null) {

            if (string.IsNullOrWhiteSpace(baseDir))
                throw new ArgumentNullException(nameof(baseDir));

            string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");
            string tempPath  = ytdlpPath + ".tmp";

            ReportProgress(progress, 0, "yt-dlp 강제 업데이트 시작...");

            try {
                // 임시 파일로 다운로드
                await DownloadToFileAsync(tempPath, progress);

                // 다운로드 성공 → 기존 파일 교체 (.NET Framework 호환: 삭제 후 이동)
                if (File.Exists(ytdlpPath))
                    File.Delete(ytdlpPath);
                File.Move(tempPath, ytdlpPath);

                ReportProgress(progress, 100, "yt-dlp 강제 업데이트 완료!");
                Console.WriteLine($"yt-dlp 강제 업데이트 완료: {ytdlpPath}");
            } catch (Exception ex) {
                // 롤백: 임시 파일만 정리 (기존 exe 는 건드리지 않음)
                DeleteFileIfExists(tempPath);
                throw new Exception($"yt-dlp 강제 업데이트 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// yt-dlp 명령어를 비동기로 실행하고 stdout 을 반환합니다.
        /// ExitCode != 0 이면 stderr 를 포함한 예외를 던집니다.
        /// </summary>
        public static async Task<string> RunYtDlpCommandAsync(
            string ytdlpPath,
            string arguments,
            int timeout = 30,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(ytdlpPath))
                throw new ArgumentNullException(nameof(ytdlpPath));
            if (!File.Exists(ytdlpPath))
                throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다.", ytdlpPath);

            var startInfo = new ProcessStartInfo {
                FileName               = ytdlpPath,
                Arguments              = arguments ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8,
            };

            using (var process = new Process { StartInfo = startInfo }) {
                var output = new StringBuilder();
                var error  = new StringBuilder();

                process.OutputDataReceived += (_, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (_, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        error.AppendLine(e.Data);
                };

                try {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool isCancelled = false;
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                        linkedCts.CancelAfter(TimeSpan.FromSeconds(timeout));

                        await Task.Run(() => {
                            while (!process.WaitForExit(500)) {
                                if (linkedCts.Token.IsCancellationRequested) {
                                    try {
                                        if (!process.HasExited)
                                            process.Kill();
                                    } catch { /* 이미 종료되었거나 실패한 경우 */ }
                                    isCancelled = true;
                                    break;
                                }
                            }

                            if (!isCancelled) {
                                // 비동기 출력 버퍼가 완전히 비워질 때까지 대기
                                process.WaitForExit();
                            }
                        });

                        if (isCancelled || linkedCts.Token.IsCancellationRequested) {
                            if (cancellationToken.IsCancellationRequested) {
                                throw new OperationCanceledException(cancellationToken);
                            }
                            throw new TimeoutException($"yt-dlp 명령 실행 시간 초과 ({timeout}초)");
                        }
                    }

                    string errorText = error.ToString();
                    if (!string.IsNullOrWhiteSpace(errorText))
                        Console.WriteLine($"yt-dlp stderr: {errorText}");

                    // ExitCode 검사
                    if (process.ExitCode != 0) {
                        throw new Exception(
                            $"yt-dlp 비정상 종료 (ExitCode={process.ExitCode}). " +
                            $"stderr: {errorText.Trim()}");
                    }

                    return output.ToString();
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception ex) when (!(ex is OperationCanceledException)) {
                    throw new Exception($"yt-dlp 명령 실행 실패: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// yt-dlp 로 비디오 정보(JSON)를 가져옵니다.
        /// </summary>
        public static async Task<string> GetVideoInfoJsonAsync(
            string ytdlpPath,
            string url,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(ytdlpPath))
                throw new ArgumentNullException(nameof(ytdlpPath));
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (!File.Exists(ytdlpPath))
                throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다.", ytdlpPath);

            string arguments = $"--dump-json --no-warnings --no-playlist \"{url}\"";

            try {
                return await RunYtDlpCommandAsync(ytdlpPath, arguments, timeout: 30, cancellationToken);
            } catch (Exception ex) {
                throw new Exception($"비디오 정보 가져오기 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 현재 실행 중인 yt-dlp 프로세스를 모두 강제 종료합니다.
        /// </summary>
        public static void KillAllYtDlpProcesses() {
            try {
                foreach (Process process in Process.GetProcessesByName("yt-dlp")) {
                    try {
                        if (!process.HasExited) {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"yt-dlp 프로세스 종료 오류: {ex.Message}");
                    } finally {
                        process?.Dispose();
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"KillAllYtDlpProcesses 오류: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────
        //  Private 핵심 로직
        // ─────────────────────────────────────────────────

        /// <summary>
        /// EnsureYtDlpAsync 의 실제 구현 (세마포어 내부에서 호출).
        /// </summary>
        private static async Task EnsureYtDlpCoreAsync(
            string baseDir,
            IProgress<FFmpegProgress> progress) {

            string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");
            string tempPath  = ytdlpPath + ".tmp";

            if (File.Exists(ytdlpPath)) {
                // ── 24시간 내 이미 업데이트를 확인한 경우 건너뛰기 ──
                DateTime lastCheck = GMTFV.Properties.Settings.Default.LastYtDlpUpdateCheck;
                if ((DateTime.Now - lastCheck).TotalHours < 24) {
                    ReportProgress(progress, 100, "yt-dlp 준비 완료 (최근 버전 확인됨)");
                    return;
                }

                // ── 24시간이 경과한 경우 업데이트 시도 ──
                ReportProgress(progress, 0, "yt-dlp 최신 버전 확인 중...");

                bool updated = await UpdateYtDlpIfNeededAsync(ytdlpPath, tempPath, progress);

                // 확인 시각 저장
                GMTFV.Properties.Settings.Default.LastYtDlpUpdateCheck = DateTime.Now;
                GMTFV.Properties.Settings.Default.Save();

                ReportProgress(progress, 100, updated
                    ? "yt-dlp가 최신 버전으로 업데이트되었습니다."
                    : "yt-dlp는 이미 최신 버전입니다.");

                return;
            }

            // ── 파일이 없으면 신규 다운로드 ──
            EnsureDirectory(baseDir);
            ReportProgress(progress, 0, "yt-dlp 다운로드 중...");

            try {
                // 임시 파일로 다운로드
                await DownloadToFileAsync(tempPath, progress);

                // 다운로드 성공 → 정식 위치로 이동 (.NET Framework 호환: 삭제 후 이동)
                if (File.Exists(ytdlpPath))
                    File.Delete(ytdlpPath);
                File.Move(tempPath, ytdlpPath);

                ReportProgress(progress, 100, "yt-dlp 다운로드 완료!");
                Console.WriteLine($"yt-dlp가 성공적으로 설치되었습니다: {ytdlpPath}");
            } catch (Exception ex) {
                // 임시 파일만 정리 (기존 exe 는 없으므로 추가 보호 불필요)
                DeleteFileIfExists(tempPath);

                throw new Exception($"yt-dlp 다운로드 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// <c>yt-dlp -U</c> 를 실행하여 업데이트를 시도합니다.
        /// </summary>
        /// <returns>업데이트가 실제로 수행되었으면 <c>true</c>, 최신 버전이거나 실패하면 <c>false</c>.</returns>
        private static async Task<bool> UpdateYtDlpIfNeededAsync(
            string ytdlpPath,
            string tempPath,
            IProgress<FFmpegProgress> progress) {

            if (!File.Exists(ytdlpPath))
                return false;

            try {
                ReportProgress(progress, 10, "yt-dlp 업데이트 확인 및 검증 중...");
                await DownloadToFileAsync(tempPath, progress);

                // 검증된 파일만 기존 실행 파일을 교체합니다.
                File.Delete(ytdlpPath);
                File.Move(tempPath, ytdlpPath);
                return true;
            } catch (Exception ex) {
                // 업데이트 실패 → 기존 exe 그대로 사용
                Console.WriteLine($"yt-dlp 업데이트 실패 (기존 버전 유지): {ex.Message}");

                ReportProgress(progress, 100, "업데이트 확인 실패 (기존 버전 사용)");

                return false;
            }
        }

        /// <summary>
        /// GitHub 에서 yt-dlp.exe 를 <paramref name="destPath"/> 로 다운로드합니다.
        /// Progress 에 진행률을 보고합니다.
        /// </summary>
        private static async Task DownloadToFileAsync(
            string destPath,
            IProgress<FFmpegProgress> progress) {

            var response = await _httpClient.GetAsync(
                DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            // FileStream: using 문으로 확실하게 Dispose
            using (var fileStream = new FileStream(
                destPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None)) {

                using (var contentStream = await response.Content.ReadAsStreamAsync()) {
                    byte[] buffer    = new byte[8192];
                    long   totalRead = 0;
                    int    bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes.HasValue && progress != null) {
                            int pct = (int)((totalRead * 100L) / totalBytes.Value);
                            progress.Report(new FFmpegProgress {
                                Percentage = Math.Min(pct, 99), // 100%는 이동 완료 후
                                Message    = $"yt-dlp 다운로드 중... {pct}%"
                            });
                        }
                    }
                }
            } // FileStream 이 닫힌 뒤에 Move 를 호출해야 함

            await FileIntegrityVerifier.VerifyRemoteSha256Async(
                destPath,
                new Uri(ChecksumUrl),
                "yt-dlp.exe");
        }

        // ─────────────────────────────────────────────────
        //  Private 헬퍼
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 정적 HttpClient 를 초기화합니다 (한 번만 호출됨).
        /// </summary>
        private static HttpClient CreateHttpClient() {
            var client = new HttpClient {
                Timeout = TimeSpan.FromMinutes(10)
            };
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            return client;
        }

        /// <summary>
        /// 디렉토리가 없으면 생성합니다.
        /// </summary>
        private static void EnsureDirectory(string dir) {
            try {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            } catch (Exception ex) {
                throw new Exception($"디렉토리 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 파일이 존재하면 삭제합니다. 실패해도 예외를 전파하지 않습니다.
        /// </summary>
        private static void DeleteFileIfExists(string path) {
            try {
                if (File.Exists(path))
                    File.Delete(path);
            } catch (Exception ex) {
                Console.WriteLine($"임시 파일 정리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// null 안전하게 Progress 를 보고합니다.
        /// </summary>
        private static void ReportProgress(
            IProgress<FFmpegProgress> progress,
            int percentage,
            string message) {

            progress?.Report(new FFmpegProgress {
                Percentage = percentage,
                Message    = message
            });
        }
    }
}
