using System;
using GMTFV.DownloadEngine;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.services;
using static GMTFV.tools.Tol;

namespace GMTFV.tools {
    internal static class YtDlpTool {

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
                ReportProgress(progress, 100, "yt-dlp 준비 완료");
                _ = RefreshInstalledAsync(baseDir);
                return;
            }

            // ── 파일이 없으면 신규 다운로드 ──
            EnsureDirectory(baseDir);
            ReportProgress(progress, 0, "yt-dlp 다운로드 중...");

            try {
                // 임시 파일로 다운로드
                await DownloadToFileAsync(tempPath, progress);

                // 다운로드 성공 → 정식 위치로 이동 (검증 후 원자적 교체)
                InstallVerifiedFile(tempPath, ytdlpPath);

                ReportProgress(progress, 100, "yt-dlp 다운로드 완료!");
                Console.WriteLine($"yt-dlp가 성공적으로 설치되었습니다: {ytdlpPath}");
            } catch (Exception ex) {
                // 임시 파일만 정리 (기존 exe 는 없으므로 추가 보호 불필요)
                DeleteFileIfExists(tempPath);

                throw new Exception($"yt-dlp 다운로드 실패: {ex.Message}", ex);
            }
        }

        private static async Task RefreshInstalledAsync(string baseDir) {
            await _ensureSemaphore.WaitAsync();
            try {
                if ((DateTime.Now - GMTFV.Properties.Settings.Default.LastYtDlpUpdateCheck).TotalHours < 24) return;
                string path = Path.Combine(baseDir, "yt-dlp.exe");
                await UpdateYtDlpIfNeededAsync(path, path + ".tmp", null);
                GMTFV.Properties.Settings.Default.LastYtDlpUpdateCheck = DateTime.Now;
                GMTFV.Properties.Settings.Default.Save();
            } catch (Exception ex) { new DiagnosticLog().Write("yt-dlp-update", ex.Message); }
            finally { _ensureSemaphore.Release(); }
        }

        /// <summary>
        /// 릴리스 버전을 비교하고 체크섬을 검증한 실행 파일로 교체합니다.
        /// </summary>
        /// <returns>업데이트가 실제로 수행되었으면 <c>true</c>, 최신 버전이거나 실패하면 <c>false</c>.</returns>
        private static async Task<bool> UpdateYtDlpIfNeededAsync(
            string ytdlpPath,
            string tempPath,
            IProgress<FFmpegProgress> progress) {

            if (!File.Exists(ytdlpPath))
                return false;

            try {
                string installed = await ToolDiagnostics.VersionAsync(ytdlpPath, new ProcessRunner(), CancellationToken.None);
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                using (var release = await _httpClient.GetAsync("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", timeout.Token)) {
                    release.EnsureSuccessStatusCode();
                    string tag = Newtonsoft.Json.Linq.JObject.Parse(await release.Content.ReadAsStringAsync())["tag_name"]?.ToString().TrimStart('v');
                    if (string.Equals(tag, installed, StringComparison.OrdinalIgnoreCase)) return false;
                }
                ReportProgress(progress, 10, "yt-dlp 업데이트 확인 및 검증 중...");
                await DownloadToFileAsync(tempPath, progress);

                // 검증된 파일만 기존 실행 파일을 교체합니다.
                InstallVerifiedFile(tempPath, ytdlpPath);
                return true;
            } catch (Exception ex) {
                DeleteFileIfExists(tempPath);
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

            using (var response = await _httpClient.GetAsync(
                DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead)) {

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
        }

        private static void InstallVerifiedFile(string source, string destination) {
            FileIntegrityVerifier.InstallVerifiedFile(source, destination);
            DeleteFileIfExists(source);
        }

        // ─────────────────────────────────────────────────
        //  Private 헬퍼
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 정적 HttpClient 를 초기화합니다 (한 번만 호출됨).
        /// </summary>
        private static HttpClient CreateHttpClient() {
            var client = new HttpClient {
                Timeout = TimeSpan.FromMinutes(2)
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
