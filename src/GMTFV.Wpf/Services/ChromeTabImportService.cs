using GMTFV.Core;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace GMTFV.Wpf.Services;

/// <summary>Chrome 확장이 localhost로 전송한 YouTube URL을 수신합니다.</summary>
public sealed class ChromeTabImportService : IDisposable {
    public const int Port = 43128;
    private readonly TcpListener listener = new(IPAddress.Loopback, Port);
    private readonly CancellationTokenSource cancellation = new();
    private readonly object requestSync = new();
    private ChromeTabRequest? pendingRequest;

    public event EventHandler<IReadOnlyList<string>>? TabsReceived;

    public bool Start() {
        try { listener.Start(); _ = ListenAsync(cancellation.Token); return true; }
        catch (SocketException) { return false; }
    }

    public string RequestTabs(ChromeTabRequestMode mode) {
        var request = new ChromeTabRequest(Guid.NewGuid().ToString("N"), mode.ToString().ToLowerInvariant());
        lock (requestSync) pendingRequest = request;
        return request.RequestId;
    }

    private async Task ListenAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            } catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) when (!cancellationToken.IsCancellationRequested) { }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken) {
        using (client)
        using (NetworkStream stream = client.GetStream()) {
        try {
            string request = await ReadRequestAsync(stream, cancellationToken);
            string origin = GetHeader(request, "Origin");
            bool trustedOrigin = origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase);
            string requestLine = request.Split("\r\n", 2, StringSplitOptions.None)[0];
            bool isOptions = requestLine.StartsWith("OPTIONS ", StringComparison.OrdinalIgnoreCase);
            bool isGetRequest = requestLine.StartsWith("GET /request ", StringComparison.OrdinalIgnoreCase);
            bool isPostTabs = requestLine.StartsWith("POST /tabs ", StringComparison.OrdinalIgnoreCase);
            bool accepted = trustedOrigin && (isOptions || isGetRequest || isPostTabs);
            string body;
            if (accepted && isPostTabs) {
                int bodyStart = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                (string? requestId, IReadOnlyList<string> urls) = ParseTabResponse(bodyStart >= 0 ? request[(bodyStart + 4)..] : string.Empty);
                if (!string.IsNullOrWhiteSpace(requestId)) {
                    lock (requestSync) if (pendingRequest?.RequestId == requestId) pendingRequest = null;
                }
                if (urls.Count > 0 || !string.IsNullOrWhiteSpace(requestId)) TabsReceived?.Invoke(this, urls);
                body = "{\"ok\":true}";
            } else if (accepted && isGetRequest) {
                ChromeTabRequest? pending;
                lock (requestSync) pending = pendingRequest;
                body = JsonSerializer.Serialize(new { requestId = pending?.RequestId ?? string.Empty, mode = pending?.Mode ?? string.Empty });
            } else {
                body = accepted ? string.Empty : "{\"ok\":false}";
            }
            string response = $"HTTP/1.1 {(accepted ? "200 OK" : "404 Not Found")}\r\nContent-Type: application/json; charset=utf-8\r\nAccess-Control-Allow-Origin: {origin}\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nAccess-Control-Allow-Headers: Content-Type\r\nCache-Control: no-store\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";
            byte[] bytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(bytes, cancellationToken);
        } catch (Exception) when (!cancellationToken.IsCancellationRequested) { }
        }
    }

    private static async Task<string> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken) {
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[4096];
        int contentLength = -1;
        while (buffer.Length < 1024 * 1024) {
            int read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            buffer.Write(chunk, 0, read);
            string text = Encoding.UTF8.GetString(buffer.ToArray());
            int headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd < 0) continue;
            if (contentLength < 0 && int.TryParse(GetHeader(text, "Content-Length"), out int parsed)) contentLength = parsed;
            if (contentLength <= 0 || buffer.Length >= headerEnd + 4 + contentLength) return text;
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static (string? RequestId, IReadOnlyList<string> Urls) ParseTabResponse(string body) {
        try {
            using JsonDocument document = JsonDocument.Parse(body);
            string? requestId = null;
            JsonElement values = document.RootElement;
            if (values.ValueKind == JsonValueKind.Object) {
                if (values.TryGetProperty("requestId", out JsonElement id)) requestId = id.GetString();
                if (!values.TryGetProperty("urls", out values)) return (requestId, Array.Empty<string>());
            }
            if (values.ValueKind != JsonValueKind.Array) return (requestId, Array.Empty<string>());
            string[] urls = values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString() ?? string.Empty).ToArray();
            IReadOnlyList<string> normalizedUrls = urls.Select(value => YouTubeUrl.TryNormalize(value, out string normalized) ? normalized : null).Where(value => value is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return (requestId, normalizedUrls);
        } catch (JsonException) { return (null, Array.Empty<string>()); }
    }

    private static string GetHeader(string request, string name) {
        int headerEnd = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd < 0) return string.Empty;
        foreach (string line in request[..headerEnd].Split("\r\n", StringSplitOptions.None)) if (line.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase)) return line[(name.Length + 1)..].Trim();
        return string.Empty;
    }

    public void Dispose() { cancellation.Cancel(); listener.Stop(); cancellation.Dispose(); }
}

public enum ChromeTabRequestMode { Current, Selected, All }
public sealed record ChromeTabRequest(string RequestId, string Mode);
