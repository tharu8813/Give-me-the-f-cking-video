using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GMTFV.tools {
    /// <summary>
    /// bgutil PO Token 공급자 서버의 수명과 상태를 관리합니다.
    /// 서버는 로컬 루프백 주소에서만 실행되며, 사용할 수 없을 때는 yt-dlp가
    /// PO Token이 필요 없는 클라이언트로 안전하게 폴백합니다.
    /// </summary>
    internal static class PoTokenProviderService {
        private const int Port = 4416;
        private static readonly Uri PingUri = new Uri("http://127.0.0.1:4416/ping");
        private static readonly object syncRoot = new object();
        private static Process ownedProcess;
        private static Task<bool> startupTask;

        public static bool IsAvailable { get; private set; }

        public static Task<bool> EnsureStartedAsync() {
            lock (syncRoot) {
                return startupTask ?? (startupTask = StartAsync());
            }
        }

        private static async Task<bool> StartAsync() {
            if (await IsServerReachableAsync()) {
                IsAvailable = true;
                return true;
            }

            string providerPath = Path.Combine(Tol.ToolDirectory, "pot-provider.exe");
            if (!File.Exists(providerPath)) {
                Console.WriteLine("PO Token 공급자 실행 파일이 없습니다. 폴백 모드로 진행합니다.");
                return false;
            }

            try {
                var startInfo = new ProcessStartInfo {
                    FileName = providerPath,
                    Arguments = "--port " + Port,
                    WorkingDirectory = Tol.ToolDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                ownedProcess = Process.Start(startInfo);
                for (int attempt = 0; attempt < 20; attempt++) {
                    await Task.Delay(250).ConfigureAwait(false);
                    if (await IsServerReachableAsync()) {
                        IsAvailable = true;
                        Console.WriteLine("PO Token 공급자 서버가 준비되었습니다.");
                        return true;
                    }

                    if (ownedProcess == null || ownedProcess.HasExited) {
                        break;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("PO Token 공급자 시작 실패: " + ex.Message);
            }

            IsAvailable = false;
            return false;
        }

        private static async Task<bool> IsServerReachableAsync() {
            try {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) }) {
                    HttpResponseMessage response = await client.GetAsync(PingUri).ConfigureAwait(false);
                    return response.IsSuccessStatusCode;
                }
            } catch {
                return false;
            }
        }

        public static void Stop() {
            IsAvailable = false;
            Process process;
            lock (syncRoot) {
                process = ownedProcess;
                ownedProcess = null;
                startupTask = null;
            }

            if (process == null) return;
            try {
                if (!process.HasExited) process.Kill();
            } catch (Exception ex) {
                Console.WriteLine("PO Token 공급자 종료 실패: " + ex.Message);
            } finally {
                process.Dispose();
            }
        }
    }
}
