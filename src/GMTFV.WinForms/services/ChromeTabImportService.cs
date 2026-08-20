using GMTFV.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.services {
    /// <summary>Chrome 확장이 현재 실행 중인 앱에 탭 주소만 전달하는 로컬 수신기입니다.</summary>
    public sealed class ChromeTabImportService : IDisposable {
        public const int Port = 43128;
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, Port);
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public event EventHandler<IReadOnlyList<string>> TabsReceived;

        public bool Start() {
            try {
                listener.Start();
                _ = ListenAsync(cancellationTokenSource.Token);
                return true;
            } catch (SocketException) {
                return false;
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client, cancellationToken);
                } catch (ObjectDisposedException) { break; } catch (SocketException) when (!cancellationToken.IsCancellationRequested) { }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken) {
            using (client)
            using (NetworkStream stream = client.GetStream()) {
                try {
                    string request = await ReadRequestAsync(stream, cancellationToken);
                    string origin = GetHeader(request, "Origin");
                    bool accepted = request.StartsWith("POST /tabs ", StringComparison.OrdinalIgnoreCase)
                        && origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase);
                    if (accepted) {
                        int bodyStart = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                        List<string> urls = ParseYouTubeUrls(bodyStart >= 0 ? request.Substring(bodyStart + 4) : string.Empty);
                        if (urls.Count > 0) TabsReceived?.Invoke(this, urls);
                    }

                    string responseBody = accepted ? "{\"ok\":true}" : "{\"ok\":false}";
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 " + (accepted ? "200 OK" : "404 Not Found") + "\r\nContent-Type: application/json; charset=utf-8\r\nAccess-Control-Allow-Origin: " + origin + "\r\nContent-Length: " + Encoding.UTF8.GetByteCount(responseBody) + "\r\n\r\n" + responseBody);
                    await stream.WriteAsync(response, 0, response.Length, cancellationToken);
                } catch (Exception) when (!cancellationToken.IsCancellationRequested) { }
            }
        }

        private static async Task<string> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken) {
            using (MemoryStream buffer = new MemoryStream()) {
                byte[] chunk = new byte[4096];
                int contentLength = -1;
                while (buffer.Length < 1024 * 1024) {
                    int read = await stream.ReadAsync(chunk, 0, chunk.Length, cancellationToken);
                    if (read == 0) break;
                    buffer.Write(chunk, 0, read);
                    string text = Encoding.UTF8.GetString(buffer.ToArray());
                    int headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd < 0) continue;
                    if (contentLength < 0) {
                        foreach (string line in text.Substring(0, headerEnd).Split(new[] { "\r\n" }, StringSplitOptions.None)) {
                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) {
                                int.TryParse(line.Substring("Content-Length:".Length).Trim(), out contentLength);
                                break;
                            }
                        }
                    }
                    if (contentLength <= 0 || buffer.Length >= headerEnd + 4 + contentLength) return text;
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        private static List<string> ParseYouTubeUrls(string body) {
            List<string> urls = new List<string>();
            try {
                foreach (string value in JArray.Parse(body).Values<string>()) {
                    if (YouTubeUrl.TryNormalize(value, out string normalizedUrl) && !urls.Contains(normalizedUrl)) urls.Add(normalizedUrl);
                }
            } catch { }
            return urls;
        }

        private static string GetHeader(string request, string name) {
            int headerEnd = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd < 0) return string.Empty;
            foreach (string line in request.Substring(0, headerEnd).Split(new[] { "\r\n" }, StringSplitOptions.None)) {
                if (line.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase)) return line.Substring(name.Length + 1).Trim();
            }
            return string.Empty;
        }

        public void Dispose() {
            cancellationTokenSource.Cancel();
            listener.Stop();
            cancellationTokenSource.Dispose();
        }
    }
}
