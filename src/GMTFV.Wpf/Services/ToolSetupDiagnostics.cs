using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using GMTFV.DownloadEngine;

namespace GMTFV.Wpf.Services;

public enum ToolSetupError { Unknown, Dns, Connection, Tls, Proxy, Forbidden, RateLimited, Server, ConnectTimeout, IdleTimeout, DownloadTimeout, Permission, DiskFull, Locked, Antivirus, Archive, Checksum, Execution, Path }
public sealed record ToolSetupFailure(ToolSetupError Kind, string Message, bool Retryable = false);
public sealed class ToolTransferTimeoutException(ToolSetupError kind, string message) : TimeoutException(message)
{
    public ToolSetupError Kind { get; } = kind;
}
public sealed class ToolRateLimitException(HttpStatusCode status, string message) : HttpRequestException(message, null, status);
public static class ToolSetupDiagnostics
{
    public static ToolSetupFailure Classify(Exception exception)
    {
        for (Exception? ex = exception; ex != null; ex = ex.InnerException) {
            int code = ex is Win32Exception win32 ? win32.NativeErrorCode : ex.HResult & 0xffff;
            if (ex is ToolTransferTimeoutException timeout) return new(timeout.Kind, timeout.Message + " 연결 상태를 확인하고 다시 시도하세요.", true);
            if (ex is CryptographicException) return new(ToolSetupError.Checksum, "받은 파일이 배포처의 정보와 달라 사용하지 않았습니다. 도구를 새로 받아 주세요.");
            if (ex is PathTooLongException || code == 206) return new(ToolSetupError.Path, "도구 경로가 너무 깁니다. 더 짧은 로컬 폴더를 선택하세요.");
            if (code is 225 or 226) return new(ToolSetupError.Antivirus, "Windows가 악성코드 감지 코드(" + code + ")를 반환했습니다. Windows 보안의 보호 기록을 확인하세요.");
            if (code is 32 or 33) return new(ToolSetupError.Locked, "다른 프로세스가 설치 파일을 사용하고 있습니다. 해당 프로그램을 닫고 다시 시도하세요.");
            if (code is 112 or 39) return new(ToolSetupError.DiskFull, "도구 저장 디스크의 공간이 부족합니다. 여유 공간을 확보하세요.");
            if (ex is UnauthorizedAccessException || code == 5) return new(ToolSetupError.Permission, "도구 폴더에 접근할 수 없습니다. 사용자 계정에 쓰기 권한이 있는 로컬 폴더를 선택하세요.");
            if (ex is HttpRequestException http) {
                if (ex is ToolRateLimitException) return new(ToolSetupError.RateLimited, "도구 배포처의 이용 한도에 도달했습니다. 잠시 후 다시 시도해 주세요. 다시 시도할 수 있는 시각은 자세한 정보에서 확인할 수 있어요.", true);
                if (http.StatusCode == HttpStatusCode.TooManyRequests) return new(ToolSetupError.RateLimited, "도구 배포처에 요청이 몰렸습니다. 잠시 기다린 뒤 다시 시도해 주세요.", true);
                if (http.StatusCode == HttpStatusCode.Forbidden) return new(ToolSetupError.Forbidden, "도구 배포처에 접근할 수 없습니다. 잠시 후 다시 시도하거나 다른 네트워크에 연결해 보세요.");
                if (http.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) return new(ToolSetupError.Proxy, "사용 중인 네트워크에 로그인이 필요합니다. 회사나 학교의 네트워크 설정을 확인해 주세요.");
                if ((int?)http.StatusCode >= 500) return new(ToolSetupError.Server, "도구 배포처에 일시적인 문제가 생겼습니다. 잠시 후 다시 시도해 주세요.", true);
                if (http.HttpRequestError == HttpRequestError.SecureConnectionError) return new(ToolSetupError.Tls, "배포처와 안전하게 연결하지 못했습니다. PC의 날짜와 시간을 확인하고 다시 시도해 주세요.");
                if (http.HttpRequestError == HttpRequestError.NameResolutionError) return new(ToolSetupError.Dns, "도구 배포처를 찾지 못했습니다. 인터넷 연결을 확인하고 다시 시도해 주세요.", true);
                if (http.HttpRequestError == HttpRequestError.ProxyTunnelError) return new(ToolSetupError.Proxy, "프록시 연결에 실패했습니다. 시스템 프록시와 방화벽 정책을 확인하세요.");
                if (http.StatusCode.HasValue) return new(ToolSetupError.Connection, "배포 요청에 실패했습니다(HTTP " + (int)http.StatusCode + "). 공식 배포 상태를 확인하세요.");
                if (ex.InnerException == null) return new(ToolSetupError.Connection, "네트워크 연결이 끊겼습니다. 인터넷, 프록시와 방화벽 접근 정책을 확인하세요.", true);
            }
            if (ex is AuthenticationException) return new(ToolSetupError.Tls, "TLS 인증서 또는 보안 연결 확인에 실패했습니다. 시스템 시간과 인증서를 확인하세요.");
            if (ex is SocketException socket) return socket.SocketErrorCode is SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData
                ? new(ToolSetupError.Dns, "DNS 조회에 실패했습니다. 인터넷과 DNS 설정을 확인하세요.", true)
                : new(ToolSetupError.Connection, "서버에 연결할 수 없습니다. 인터넷, 프록시와 방화벽 접근 정책을 확인하세요.", true);
            if (ex is HttpIOException) return new(ToolSetupError.Connection, "데이터 전송 도중 연결이 끊겼습니다. 인터넷 연결을 확인한 뒤 다시 시도하세요.", true);
            if (ex is ToolExecutionException || ex is Win32Exception || ex is TimeoutException) return new(ToolSetupError.Execution, "받은 도구를 정상적으로 실행하지 못했습니다. 도구를 새로 받아 주세요. 문제가 계속되면 문제 해결 기록을 확인해 주세요.");
        }
        if (exception is HttpRequestException) return new(ToolSetupError.Connection, "네트워크 연결에 실패했습니다. 프록시와 방화벽 정책을 확인하세요.", true);
        if (exception is InvalidOperationException && (exception.Message.Contains("버전") || exception.Message.Contains("Deno"))) return new(ToolSetupError.Execution, "받은 도구가 이 앱에서 사용할 수 있는 버전이 아닙니다. 도구를 새로 받아 주세요.");
        return exception is InvalidDataException
            ? new(ToolSetupError.Archive, "배포 정보나 압축 파일이 손상되었거나 예상 구성과 다릅니다. 공식 배포 상태를 확인하고 다시 시도하세요.")
            : new(ToolSetupError.Unknown, "다시 시도해 주세요. 문제가 계속되면 아래의 문제 해결 기록과 자세한 정보를 확인해 주세요.");
    }
}

public sealed record ToolSetupProgress(string Tool, int Step, string Phase, long Bytes = 0, long? TotalBytes = null, double BytesPerSecond = 0)
{
    public int? Percent => TotalBytes is > 0 ? (int)Math.Min(100, 100d * Bytes / TotalBytes.Value) : null;
    public string Text => $"{Step}/8 · {Tool} · {Phase}" + (Phase == "다운로드 중" ?
        (Percent is int p ? $" · {p}%" : "") + $" · {Bytes / 1048576d:F1}" + (TotalBytes is long size ? $" / {size / 1048576d:F1}" : "") + " MB" +
        (BytesPerSecond > 0 ? $" · {BytesPerSecond / 1048576d:F1} MB/s" + (TotalBytes is long total && total >= Bytes ? $" · 약 {Math.Ceiling((total - Bytes) / BytesPerSecond)}초 남음" : "") : "") : "");
}
