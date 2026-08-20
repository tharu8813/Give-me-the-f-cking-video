using System;

namespace GMTFV.Core {
    /// <summary>다운로드 UI가 표현하는 안정적인 작업 단계입니다.</summary>
    public enum DownloadPhase {
        Queued,
        Video,
        Audio,
        Merging,
        Processing,
        Finishing,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>UI가 외부 도구에 의존하지 않고 표시할 수 있는 진행 상태입니다.</summary>
    public sealed class DownloadProgress {
        public DownloadProgress(DownloadPhase phase, int? percent, string message, string speed = "") {
            Phase = phase;
            Percent = percent.HasValue ? Math.Max(0, Math.Min(100, percent.Value)) : (int?)null;
            Message = message ?? string.Empty;
            Speed = speed ?? string.Empty;
        }

        public DownloadPhase Phase { get; private set; }
        public int? Percent { get; private set; }
        public string Message { get; private set; }
        public string Speed { get; private set; }
    }

    /// <summary>영상·오디오 출력 선택을 표현하는 UI 독립 계약입니다.</summary>
    public sealed class DownloadProfile {
        public bool IsVideo { get; set; }
        public string Container { get; set; }
        public string Quality { get; set; }
        public int FramesPerSecond { get; set; }
    }

    /// <summary>가져오기/내보내기에 사용되는 UI 독립 목록 항목입니다.</summary>
    public sealed class PlaylistItem {
        public string Url { get; set; }
        public DownloadProfile Profile { get; set; }
    }
}
