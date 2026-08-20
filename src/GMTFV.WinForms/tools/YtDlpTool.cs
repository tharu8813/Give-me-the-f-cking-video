using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.tools {
    internal class YtDlpTool {

        // ─────────────────────────────────────────────────
        //  Constants
        // ─────────────────────────────────────────────────

        private const string YtDlpFileName = "yt-dlp.exe";
        private const string DenoFileName = "deno.exe";


        // ─────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────

        /// <summary>
        /// yt-dlp.exe가 존재하는지 확인합니다.
        /// </summary>
        public static bool IsYtDlpInstalled(string baseDir) {
            if (string.IsNullOrWhiteSpace(baseDir))
                return false;

            try {
                return File.Exists(
                    Path.Combine(baseDir, YtDlpFileName));
            } catch (Exception ex) {
                Console.WriteLine(
                    $"IsYtDlpInstalled 오류: {ex.Message}");

                return false;
            }
        }


        /// <summary>
        /// yt-dlp 명령어를 비동기로 실행하고 stdout을 반환합니다.
        /// ExitCode != 0이면 stderr를 포함한 예외를 던집니다.
        /// </summary>
        public static async Task<string> RunYtDlpCommandAsync(
            string ytdlpPath,
            string arguments,
            int timeout = 30,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(ytdlpPath))
                throw new ArgumentNullException(nameof(ytdlpPath));

            if (timeout <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    "timeout은 1초 이상이어야 합니다.");

            // 호출하기 전에 이미 취소되었다면 바로 종료
            cancellationToken.ThrowIfCancellationRequested();

            string fullYtDlpPath;

            try {
                fullYtDlpPath = Path.GetFullPath(ytdlpPath);
            } catch (Exception ex) {
                throw new ArgumentException(
                    "올바르지 않은 yt-dlp 경로입니다.",
                    nameof(ytdlpPath),
                    ex);
            }

            if (!File.Exists(fullYtDlpPath)) {
                throw new FileNotFoundException(
                    "yt-dlp.exe를 찾을 수 없습니다. " +
                    "프로그램을 다시 설치해주세요.",
                    fullYtDlpPath);
            }

            // Deno Runtime 탐색
            string denoPath = FindDenoPath(fullYtDlpPath);

            if (denoPath == null) {
                throw new FileNotFoundException(
                    "Deno JavaScript Runtime을 찾을 수 없습니다. " +
                    "프로그램을 다시 설치해주세요.",
                    DenoFileName);
            }

            // 앱에 동봉한 bgutil 공급자만 명시적으로 로드합니다.
            // 사용자/이전 빌드의 플러그인은 차단해 nodriver 등 미설치 의존성 오류를 방지합니다.
            string pluginArgument = PoTokenProviderService.IsAvailable
                ? "--no-plugin-dirs --plugin-dirs " + QuoteCommandLineArgument(Tol.YtDlpPluginDirectory)
                : "--no-plugin-dirs";
            string safeRuntimeArguments =
                "--ignore-config " + pluginArgument + " --js-runtimes " +
                QuoteCommandLineArgument($"deno:{denoPath}");

            string finalArguments = string.IsNullOrWhiteSpace(arguments)
                ? safeRuntimeArguments
                : $"{safeRuntimeArguments} {arguments}";

            string workingDirectory =
                Path.GetDirectoryName(fullYtDlpPath);

            if (string.IsNullOrWhiteSpace(workingDirectory)) {
                workingDirectory =
                    AppDomain.CurrentDomain.BaseDirectory;
            }

            var startInfo = new ProcessStartInfo {
                FileName = fullYtDlpPath,
                Arguments = finalArguments,

                WorkingDirectory = workingDirectory,

                RedirectStandardOutput = true,
                RedirectStandardError = true,

                UseShellExecute = false,
                CreateNoWindow = true,

                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = new Process()) {
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;

                var output = new StringBuilder();
                var error = new StringBuilder();

                // 프로세스 종료 확인
                var processExited =
                    new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                // stdout 읽기 완료 확인
                var outputClosed =
                    new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                // stderr 읽기 완료 확인
                var errorClosed =
                    new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);


                process.OutputDataReceived += (_, e) => {
                    if (e.Data == null) {
                        outputClosed.TrySetResult(true);
                        return;
                    }

                    output.AppendLine(e.Data);
                };


                process.ErrorDataReceived += (_, e) => {
                    if (e.Data == null) {
                        errorClosed.TrySetResult(true);
                        return;
                    }

                    error.AppendLine(e.Data);
                };


                process.Exited += (_, __) => {
                    processExited.TrySetResult(true);
                };


                try {
                    if (!process.Start()) {
                        throw new InvalidOperationException(
                            "yt-dlp 프로세스를 시작할 수 없습니다.");
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 극히 드문 race condition 방지
                    if (process.HasExited)
                        processExited.TrySetResult(true);


                    using (var timeoutCts =
                           new CancellationTokenSource()) {

                        timeoutCts.CancelAfter(
                            TimeSpan.FromSeconds(timeout));

                        using (var linkedCts =
                               CancellationTokenSource
                                   .CreateLinkedTokenSource(
                                       cancellationToken,
                                       timeoutCts.Token)) {

                            // CancellationToken을 Task로 변환
                            Task cancellationTask =
                                Task.Delay(
                                    Timeout.Infinite,
                                    linkedCts.Token);

                            Task completedTask =
                                await Task.WhenAny(
                                        processExited.Task,
                                        cancellationTask)
                                    .ConfigureAwait(false);


                            // 정상적으로 프로세스가 종료되지 않은 경우
                            if (completedTask != processExited.Task) {

                                TryKillProcess(process);

                                // 사용자에 의한 취소
                                if (cancellationToken
                                    .IsCancellationRequested) {

                                    throw new OperationCanceledException(
                                        "yt-dlp 명령 실행이 취소되었습니다.",
                                        cancellationToken);
                                }

                                // timeout 발생
                                throw new TimeoutException(
                                    $"yt-dlp 명령 실행 시간 초과 " +
                                    $"({timeout}초)");
                            }
                        }
                    }


                    // stdout / stderr의 비동기 읽기가
                    // 완전히 끝날 때까지 대기
                    await Task.WhenAll(
                            outputClosed.Task,
                            errorClosed.Task)
                        .ConfigureAwait(false);


                    string outputText = output.ToString();
                    string errorText = error.ToString();


                    if (!string.IsNullOrWhiteSpace(errorText)) {
                        Console.WriteLine(
                            $"yt-dlp stderr:{Environment.NewLine}" +
                            errorText.Trim());
                    }


                    if (process.ExitCode != 0) {
                        string stderr =
                            string.IsNullOrWhiteSpace(errorText)
                                ? "없음"
                                : errorText.Trim();

                        throw new Exception(
                            $"yt-dlp 비정상 종료 " +
                            $"(ExitCode={process.ExitCode}). " +
                            $"stderr: {stderr}");
                    }


                    return outputText;

                } catch (OperationCanceledException) {

                    TryKillProcess(process);
                    throw;

                } catch (TimeoutException) {

                    TryKillProcess(process);
                    throw;

                } catch (Exception ex) {

                    TryKillProcess(process);

                    throw new Exception(
                        $"yt-dlp 명령 실행 실패: {ex.Message}",
                        ex);
                }
            }
        }


        /// <summary>
        /// yt-dlp로 비디오 정보(JSON)를 가져옵니다.
        /// </summary>
        public static async Task<string> GetVideoInfoJsonAsync(
            string ytdlpPath,
            string url,
            CancellationToken cancellationToken = default) {

            if (string.IsNullOrWhiteSpace(ytdlpPath))
                throw new ArgumentNullException(nameof(ytdlpPath));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            cancellationToken.ThrowIfCancellationRequested();

            string arguments =
                "--dump-json " +
                "--no-warnings " +
                "--no-playlist " +
                GetYoutubeClientArguments() + " " +
                QuoteCommandLineArgument(url);

            try {
                return await RunYtDlpCommandAsync(
                        ytdlpPath,
                        arguments,
                        timeout: 30,
                        cancellationToken)
                    .ConfigureAwait(false);

            } catch (OperationCanceledException) {
                throw;

            } catch (TimeoutException) {
                throw;

            } catch (Exception ex) {
                throw new Exception(
                    $"비디오 정보 가져오기 실패: {ex.Message}",
                    ex);
            }
        }

        internal static string GetYoutubeClientArguments() {
            return PoTokenProviderService.IsAvailable
                ? "--extractor-args \"youtube:player_client=mweb\""
                : "--extractor-args \"youtube:player_client=tv,web_embedded,android_vr\"";
        }


        /// <summary>
        /// 현재 실행 중인 yt-dlp 프로세스를 모두 강제 종료합니다.
        /// </summary>
        public static void KillAllYtDlpProcesses() {
            try {
                Process[] processes =
                    Process.GetProcessesByName("yt-dlp");

                foreach (Process process in processes) {
                    try {
                        TryKillProcess(process);

                    } catch (Exception ex) {
                        Console.WriteLine(
                            $"yt-dlp 프로세스 종료 오류: " +
                            $"{ex.Message}");

                    } finally {
                        process.Dispose();
                    }
                }

            } catch (Exception ex) {
                Console.WriteLine(
                    $"KillAllYtDlpProcesses 오류: {ex.Message}");
            }
        }


        // ─────────────────────────────────────────────────
        //  Private Helpers
        // ─────────────────────────────────────────────────

        /// <summary>
        /// Deno Runtime의 위치를 탐색합니다.
        ///
        /// 탐색 순서:
        /// 1. yt-dlp.exe와 같은 폴더
        /// 2. 프로그램 실행 파일 폴더
        /// 3. 현재 작업 폴더
        /// </summary>
        private static string FindDenoPath(string ytdlpPath) {

            // 1. yt-dlp.exe 위치
            string ytdlpDirectory =
                Path.GetDirectoryName(ytdlpPath);

            if (!string.IsNullOrWhiteSpace(ytdlpDirectory)) {
                string path =
                    Path.Combine(
                        ytdlpDirectory,
                        DenoFileName);

                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }


            // 2. 프로그램 실행 파일 위치
            string appDirectory =
                AppDomain.CurrentDomain.BaseDirectory;

            if (!string.IsNullOrWhiteSpace(appDirectory)) {
                string path =
                    Path.Combine(
                        appDirectory,
                        DenoFileName);

                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }


            // 3. 현재 Working Directory
            try {
                string path =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        DenoFileName);

                if (File.Exists(path))
                    return Path.GetFullPath(path);

            } catch {
                // CurrentDirectory를 가져올 수 없는 경우 무시
            }


            return null;
        }


        /// <summary>
        /// 프로세스가 실행 중이면 안전하게 종료합니다.
        /// </summary>
        private static void TryKillProcess(Process process) {
            if (process == null)
                return;

            try {
                if (process.HasExited)
                    return;

                process.Kill();

                try {
                    process.WaitForExit(3000);
                } catch {
                    // 종료 대기 실패는 무시
                }

            } catch (InvalidOperationException) {
                // 이미 종료된 프로세스

            } catch (Exception ex) {
                Console.WriteLine(
                    $"프로세스 종료 실패: {ex.Message}");
            }
        }


        /// <summary>
        /// Windows CommandLine 인자를 안전하게 따옴표 처리합니다.
        /// </summary>
        private static string QuoteCommandLineArgument(
            string argument) {

            if (argument == null)
                return "\"\"";

            if (argument.Length == 0)
                return "\"\"";


            var result = new StringBuilder();

            result.Append('"');

            int backslashCount = 0;

            foreach (char c in argument) {

                if (c == '\\') {
                    backslashCount++;
                    continue;
                }


                if (c == '"') {

                    // 따옴표 앞의 '\'는 2배 처리
                    result.Append(
                        '\\',
                        backslashCount * 2 + 1);

                    result.Append('"');

                    backslashCount = 0;
                    continue;
                }


                if (backslashCount > 0) {
                    result.Append(
                        '\\',
                        backslashCount);

                    backslashCount = 0;
                }

                result.Append(c);
            }


            // 마지막 따옴표 바로 앞의 '\'는
            // 모두 2배 처리해야 함
            if (backslashCount > 0) {
                result.Append(
                    '\\',
                    backslashCount * 2);
            }

            result.Append('"');

            return result.ToString();
        }
    }
}
