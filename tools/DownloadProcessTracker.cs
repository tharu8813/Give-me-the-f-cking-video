using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GMTFV.tools {
    /// <summary>
    /// 다운로드 중 실행되는 CLI 프로세스 수명주기 추적 및 강제 종료 전담 유틸리티
    /// </summary>
    public class DownloadProcessTracker {
        private readonly List<Process> _activeProcesses = new List<Process>();
        private readonly object _lock = new object();

        public void RegisterProcess(Process p) {
            if (p == null) return;
            lock (_lock) {
                if (!_activeProcesses.Contains(p)) {
                    _activeProcesses.Add(p);
                }
            }
        }

        public void UnregisterProcess(Process p) {
            if (p == null) return;
            lock (_lock) {
                _activeProcesses.Remove(p);
            }
        }

        public void KillAllActiveProcesses() {
            lock (_lock) {
                foreach (var p in _activeProcesses.ToList()) {
                    try {
                        if (p != null && !p.HasExited) {
                            p.Kill();
                        }
                    } catch { }
                }
                _activeProcesses.Clear();
            }

            // 시스템 레벨의 잔류 프로세스도 확실히 종결
            KillProcessesByName("yt-dlp");
            KillProcessesByName("ffmpeg");
        }

        public static void KillProcessesByName(string processName) {
            try {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes) {
                    try {
                        if (!process.HasExited) {
                            process.Kill();
                            process.WaitForExit(1000);
                        }
                    } catch { } finally {
                        process.Dispose();
                    }
                }
            } catch { }
        }
    }
}
