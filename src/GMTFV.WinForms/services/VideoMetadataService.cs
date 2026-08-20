using GMTFV.Core;
using GMTFV.tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GMTFV.services {
    /// <summary>유튜브 URL 정규화, yt-dlp 메타데이터 조회와 썸네일 조회를 담당합니다.</summary>
    internal sealed class VideoMetadataService {
        private readonly HttpClient httpClient;

        public VideoMetadataService(HttpClient httpClient = null) {
            this.httpClient = httpClient ?? CreateHttpClient();
        }

        public string NormalizeYouTubeUrl(string url) {
            return YouTubeUrl.Normalize(url);
        }

        public async Task<VideoMetadata> GetAsync(string normalizedUrl, string toolDirectory, CancellationToken cancellationToken) {
            string ytdlpPath = Path.Combine(toolDirectory ?? string.Empty, "yt-dlp.exe");
            if (!File.Exists(ytdlpPath))
                throw new FileNotFoundException("yt-dlp.exe를 찾을 수 없습니다. 프로그램 설치 파일을 다시 실행해 복구해주세요.", ytdlpPath);

            try {
                string jsonOutput = await YtDlpTool.GetVideoInfoJsonAsync(ytdlpPath, normalizedUrl, cancellationToken);
                if (string.IsNullOrWhiteSpace(jsonOutput))
                    throw new InvalidOperationException("yt-dlp에서 데이터를 받지 못했습니다.");

                return Parse(jsonOutput);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                string error = ex.Message;
                if (error.Contains("Video unavailable"))
                    throw new InvalidOperationException("영상을 사용할 수 없습니다. (비공개 또는 삭제됨)", ex);
                if (error.Contains("Private video"))
                    throw new InvalidOperationException("비공개 영상입니다.", ex);
                if (error.IndexOf("age", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("연령 제한이 있는 영상입니다.", ex);
                throw new InvalidOperationException("영상 정보 가져오기 실패: " + error, ex);
            }
        }

        public async Task<Image> DownloadThumbnailAsync(string videoId, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(videoId)) return null;

            byte[] imageBytes = null;
            try {
                imageBytes = await httpClient.GetByteArrayAsync("https://i.ytimg.com/vi/" + videoId + "/maxresdefault.jpg");
            } catch {
                try {
                    imageBytes = await httpClient.GetByteArrayAsync("https://i.ytimg.com/vi/" + videoId + "/0.jpg");
                } catch {
                    return null;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = new MemoryStream(imageBytes))
            using (var source = Image.FromStream(stream)) {
                return new Bitmap(source);
            }
        }

        private static VideoMetadata Parse(string json) {
            try {
                JObject data = JObject.Parse(json);
                var metadata = new VideoMetadata {
                    Title = data["title"]?.ToString() ?? "제목 없음",
                    Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                    Uploader = data["uploader"]?.ToString() ?? data["channel"]?.ToString() ?? "알 수 없음",
                    Duration = TimeSpan.FromSeconds(data["duration"]?.ToObject<double>() ?? 0),
                    UploadDate = ParseUploadDate(data["upload_date"]?.ToString()),
                    Formats = new List<VideoFormat>()
                };

                foreach (JObject format in (data["formats"] as JArray ?? new JArray()).OfType<JObject>()) {
                    metadata.Formats.Add(new VideoFormat {
                        Height = format["height"]?.ToObject<int?>(),
                        Fps = format["fps"]?.ToObject<int?>()
                    });
                }
                return metadata;
            } catch (Exception ex) {
                throw new InvalidOperationException("JSON 파싱 오류: " + ex.Message, ex);
            }
        }

        private static DateTime ParseUploadDate(string value) {
            return DateTime.TryParseExact(value, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out DateTime date) ? date : DateTime.Now;
        }

        private static HttpClient CreateHttpClient() {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            return client;
        }
    }

    internal sealed class VideoMetadata {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Uploader { get; set; }
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public List<VideoFormat> Formats { get; set; }
    }

    internal sealed class VideoFormat {
        public int? Height { get; set; }
        public int? Fps { get; set; }
    }
}
