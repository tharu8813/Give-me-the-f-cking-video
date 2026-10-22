using System;
using System.IO;
using GMTFV.Core;

namespace GMTFV.DownloadEngine {
    public static class RecoveryPolicy {
        public const int MaxAttempts = 3;
        public static DownloadError Classify(Exception exception) {
            if (exception is OperationCanceledException) return DownloadError.Cancelled;
            if (exception is TimeoutException) return DownloadError.Timeout;
            if (exception is UnauthorizedAccessException) return DownloadError.Permission;
            if (exception is FileNotFoundException) return DownloadError.MissingTool;
            if (exception is InvalidDataException) return DownloadError.InvalidOutput;
            if (exception is IOException && ((exception.HResult & 0xffff) == 112 || (exception.HResult & 0xffff) == 39)) return DownloadError.DiskFull;
            string text = ((exception as ToolExecutionException)?.Error ?? exception.Message ?? "").ToLowerInvariant();
            if (Has(text, "no space left", "disk full", "not enough space")) return DownloadError.DiskFull;
            if (Has(text, "permission denied", "access is denied", "used by another process", "read-only file")) return DownloadError.Permission;
            if (Has(text, "429", "too many requests", "rate limit")) return DownloadError.RateLimited;
            if (Has(text, "private video", "video unavailable", "has been removed", "members-only", "sign in", "age-restricted", "not available in your country", "drm", "copyright")) return DownloadError.Unavailable;
            if (Has(text, "po token", "po_token", "pot provider")) return DownloadError.PoToken;
            if (Has(text, "javascript runtime", "challenge solving", "n challenge", "signature extraction", "unsupported client", "deno", "unrecognized arguments")) return DownloadError.Client;
            if (Has(text, "403", "forbidden")) return DownloadError.Forbidden;
            if (Has(text, "requested format", "no video formats", "format is not available")) return DownloadError.Format;
            if (Has(text, "ffmpeg", "ffprobe", "postprocessing", "error opening encoder", "nvenc", "cuda")) return DownloadError.FFmpeg;
            if (Has(text, "timed out", "timeout")) return DownloadError.Timeout;
            if (Has(text, "connection reset", "connection refused", "temporary failure", "remote end closed", "unable to download", "network is unreachable", "name resolution", "http error 5")) return DownloadError.Network;
            return DownloadError.Unknown;
        }

        public static bool ShouldRetry(DownloadError error, int attempt, bool qualityFallback, bool gpu) {
            if (attempt >= MaxAttempts) return false;
            if (error == DownloadError.Network || error == DownloadError.Timeout) return true;
            return attempt == 1 && (error == DownloadError.Forbidden || error == DownloadError.PoToken ||
                (error == DownloadError.Format && qualityFallback) || (error == DownloadError.FFmpeg && gpu));
        }
        public static TimeSpan Delay(int attempt) => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt)));

        public static string Message(DownloadError error) {
            switch (error) {
                case DownloadError.Network: case DownloadError.Timeout: return "네트워크 연결이 불안정하거나 응답이 늦습니다. 연결을 확인한 뒤 다시 시도하세요.";
                case DownloadError.Forbidden: return "YouTube에서 영상을 받을 수 없습니다. 잠시 후 다시 시도하거나 도구 및 진단에서 도구를 새로 받아 주세요.";
                case DownloadError.RateLimited: return "서버가 요청 횟수를 제한했습니다. 자동 재시도를 중단했으니 충분히 기다린 뒤 다시 시도하세요.";
                case DownloadError.Client: return "YouTube 영상 정보를 처리하지 못했습니다. 도구 및 진단에서 준비 상태를 확인하고 필요한 도구를 새로 받아 주세요.";
                case DownloadError.PoToken: return "YouTube에서 다운로드를 허용하는지 확인하지 못했습니다. 도구 및 진단에서 도구를 새로 받거나 잠시 후 다시 시도해 주세요.";
                case DownloadError.Format: return "요청한 화질이나 형식을 사용할 수 없습니다. 다른 화질 또는 형식을 선택하세요.";
                case DownloadError.FFmpeg: return "영상이나 오디오를 합치고 변환하는 중 문제가 생겼습니다. 도구 및 진단에서 준비 상태를 확인하거나 다른 저장 형식으로 시도해 주세요.";
                case DownloadError.Permission: return "저장 파일에 접근할 수 없습니다. 쓰기 권한과 파일 사용 여부를 확인하세요.";
                case DownloadError.DiskFull: return "디스크 공간이 부족합니다. 저장 공간을 확보하세요.";
                case DownloadError.Unavailable: return "비공개·삭제·접근 제한 영상입니다. 영상 상태와 본인의 접근 권한을 확인하세요.";
                case DownloadError.MissingTool: return "다운로드에 필요한 도구가 없습니다. 도구 및 진단에서 ‘한 번에 준비하기’를 눌러 주세요.";
                case DownloadError.InvalidOutput: return "완료된 파일을 검증할 수 없습니다. 상세 로그를 확인하세요.";
                case DownloadError.Cancelled: return "다운로드가 취소되었습니다.";
                default: return "다운로드에 실패했습니다. 상세 로그를 확인한 뒤 다시 시도하세요.";
            }
        }
        private static bool Has(string text, params string[] values) { foreach (string value in values) if (text.Contains(value)) return true; return false; }
    }
}
