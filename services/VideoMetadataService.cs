using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;
using GMTFV.tools;

namespace GMTFV.services {
    internal sealed class VideoMetadataService {
        private readonly HttpClient httpClient;
        public VideoMetadataService(HttpClient httpClient = null) { this.httpClient = httpClient ?? Tol.SharedHttpClient; }
        public string NormalizeYouTubeUrl(string url) => YouTubeUrl.Normalize(url);
        public Task<GMTFV.DownloadEngine.VideoMetadata> GetAsync(string url, string toolDirectory, CancellationToken token) =>
            new GMTFV.DownloadEngine.VideoMetadataService().GetAsync(url, toolDirectory, token);

        public async Task<Image> DownloadThumbnailAsync(string videoId, CancellationToken token) {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                deadline.CancelAfter(TimeSpan.FromSeconds(10));
                foreach (string name in new[] { "maxresdefault.jpg", "0.jpg" }) {
                    try {
                        using (var response = await httpClient.GetAsync("https://i.ytimg.com/vi/" + videoId + "/" + name, deadline.Token)) {
                            response.EnsureSuccessStatusCode();
                            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                            token.ThrowIfCancellationRequested();
                            using (var stream = new MemoryStream(bytes))
                            using (var source = Image.FromStream(stream)) return new Bitmap(source);
                        }
                    } catch (HttpRequestException) { }
                    catch (ArgumentException) { }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested) { return null; }
                }
                return null;
            }
        }
    }
}
