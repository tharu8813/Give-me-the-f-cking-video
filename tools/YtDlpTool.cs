using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GMTFV.tools.Tol;

namespace GMTFV.tools {
    internal class YtDlpTool {

        // yt-dlp 다운로드 및 업데이트
        public static async Task EnsureYtDlpAsync(string baseDir, IProgress<FFmpegProgress> progress = null) {
            if (string.IsNullOrWhiteSpace(baseDir)) {
                throw new ArgumentNullException(nameof(baseDir));
            }

            string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");

            // 이미 존재하는지 확인
            if (File.Exists(ytdlpPath)) {
                progress?.Report(new FFmpegProgress {
                    Percentage = 100,
                    Message = "yt-dlp가 이미 설치되어 있습니다."
                });

                // 버전 확인 (선택사항)
                await UpdateYtDlpIfNeededAsync(ytdlpPath, progress);
                return;
            }

            // 디렉토리 생성
            try {
                if (!Directory.Exists(baseDir)) {
                    Directory.CreateDirectory(baseDir);
                }
            } catch (Exception ex) {
                throw new Exception($"디렉토리 생성 실패: {ex.Message}", ex);
            }

            progress?.Report(new FFmpegProgress {
                Percentage = 0,
                Message = "yt-dlp 다운로드 중..."
            });

            HttpClient client = null;
            FileStream fileStream = null;

            try {
                client = new HttpClient {
                    Timeout = TimeSpan.FromMinutes(10)
                };
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                // yt-dlp 최신 릴리즈 다운로드
                string downloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

                var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                fileStream = new FileStream(ytdlpPath, FileMode.Create, FileAccess.Write, FileShare.None);

                using (var contentStream = await response.Content.ReadAsStreamAsync()) {
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes.HasValue && progress != null) {
                            int percentage = (int)((totalRead * 100) / totalBytes.Value);
                            progress.Report(new FFmpegProgress {
                                Percentage = Math.Min(percentage, 100),
                                Message = $"yt-dlp 다운로드 중... {percentage}%"
                            });
                        }
                    }
                }

                fileStream.Close();
                fileStream = null;

                progress?.Report(new FFmpegProgress {
                    Percentage = 100,
                    Message = "yt-dlp 다운로드 완료!"
                });

                Console.WriteLine($"yt-dlp가 성공적으로 다운로드되었습니다: {ytdlpPath}");
            } catch (HttpRequestException ex) {
                throw new Exception($"yt-dlp 다운로드 실패 (네트워크 오류): {ex.Message}", ex);
            } catch (TaskCanceledException ex) {
                throw new Exception($"yt-dlp 다운로드 시간 초과: {ex.Message}", ex);
            } catch (Exception ex) {
                throw new Exception($"yt-dlp 다운로드 실패: {ex.Message}", ex);
            } finally {
                fileStream?.Dispose();
                client?.Dispose();

                // 다운로드 실패 시 불완전한 파일 삭제
                if (File.Exists(ytdlpPath)) {
                    try {
                        FileInfo fi = new FileInfo(ytdlpPath);
                        if (fi.Length == 0) {
                            File.Delete(ytdlpPath);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"불완전한 파일 정리 실패: {ex.Message}");
                    }
                }
            }
        }

        // yt-dlp 버전 확인 및 업데이트 (선택사항)
        private static async Task UpdateYtDlpIfNeededAsync(string ytdlpPath, IProgress<FFmpegProgress> progress) {
            if (!File.Exists(ytdlpPath)) {
                return;
            }

            try {
                // 버전 확인
                var versionInfo = await RunYtDlpCommandAsync(ytdlpPath, "--version", timeout: 10);

                if (!string.IsNullOrWhiteSpace(versionInfo)) {
                    Console.WriteLine($"현재 yt-dlp 버전: {versionInfo.Trim()}");

                    // 필요시 자동 업데이트 (주석 처리됨 - 사용자 동의 필요)
                    // await RunYtDlpCommandAsync(ytdlpPath, "-U");
                }
            } catch (Exception ex) {
                Console.WriteLine($"yt-dlp 버전 확인 실패: {ex.Message}");
            }
        }

        // yt-dlp 명령어 실행 헬퍼
        public static async Task<string> RunYtDlpCommandAsync(
            string ytdlpPath,
            string arguments,
            int timeout = 30,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(ytdlpPath)) {
                throw new ArgumentNullException(nameof(ytdlpPath));
            }

            if (!File.Exists(ytdlpPath)) {
                throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다.", ytdlpPath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = ytdlpPath,
                Arguments = arguments ?? "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Process process = null;
            try {
                process = new Process { StartInfo = startInfo };
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 타임아웃 적용
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                    cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                    await Task.Run(() => {
                        while (!process.WaitForExit(1000)) {
                            if (cts.Token.IsCancellationRequested) {
                                try {
                                    process.Kill();
                                } catch { }
                                throw new OperationCanceledException("작업 시간이 초과되었습니다.");
                            }
                        }
                    }, cts.Token);
                }

                string errorOutput = error.ToString();
                if (!string.IsNullOrWhiteSpace(errorOutput)) {
                    Console.WriteLine($"yt-dlp stderr: {errorOutput}");
                }

                return output.ToString();
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                throw new Exception($"yt-dlp 명령 실행 실패: {ex.Message}", ex);
            } finally {
                process?.Dispose();
            }
        }

        /// <summary>
        /// yt-dlp가 설치되어 있는지 확인합니다.
        /// </summary>
        public static bool IsYtDlpInstalled(string baseDir) {
            if (string.IsNullOrWhiteSpace(baseDir)) {
                return false;
            }

            try {
                string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");
                return File.Exists(ytdlpPath);
            } catch (Exception ex) {
                Console.WriteLine($"IsYtDlpInstalled 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// yt-dlp를 강제로 업데이트합니다.
        /// </summary>
        public static async Task ForceUpdateYtDlpAsync(string baseDir, IProgress<FFmpegProgress> progress = null) {
            if (string.IsNullOrWhiteSpace(baseDir)) {
                throw new ArgumentNullException(nameof(baseDir));
            }

            string ytdlpPath = Path.Combine(baseDir, "yt-dlp.exe");

            try {
                // 기존 파일 삭제
                if (File.Exists(ytdlpPath)) {
                    File.Delete(ytdlpPath);
                }

                // 재다운로드
                await EnsureYtDlpAsync(baseDir, progress);
            } catch (Exception ex) {
                throw new Exception($"yt-dlp 업데이트 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// yt-dlp로 비디오 정보를 가져옵니다.
        /// </summary>
        public static async Task<string> GetVideoInfoJsonAsync(
            string ytdlpPath,
            string url,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(ytdlpPath)) {
                throw new ArgumentNullException(nameof(ytdlpPath));
            }

            if (string.IsNullOrWhiteSpace(url)) {
                throw new ArgumentNullException(nameof(url));
            }

            if (!File.Exists(ytdlpPath)) {
                throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다.", ytdlpPath);
            }

            string arguments = $"--dump-json --no-warnings --no-playlist \"{url}\"";

            try {
                return await RunYtDlpCommandAsync(ytdlpPath, arguments, timeout: 30, cancellationToken);
            } catch (Exception ex) {
                throw new Exception($"비디오 정보 가져오기 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// yt-dlp 프로세스를 모두 종료합니다.
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
    }
}