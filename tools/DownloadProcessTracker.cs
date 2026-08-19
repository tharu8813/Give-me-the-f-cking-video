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

        }

    }
}
