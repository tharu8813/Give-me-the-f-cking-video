using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;
using Newtonsoft.Json.Linq;

namespace GMTFV.DownloadEngine {
    public sealed class VideoMetadataService {
        public async Task<VideoMetadata> GetAsync(string url, string toolDirectory, CancellationToken token) {
            ProcessResult result = await new YtDlpClient(toolDirectory).RunAsync(new[] { "--dump-single-json", "--skip-download", "--", YouTubeUrl.Normalize(url) },
                TimeSpan.FromSeconds(60), token).ConfigureAwait(false);
            return Parse(result.Output);
        }

        public static VideoMetadata Parse(string json) {
            try {
                JObject data = JObject.Parse(json);
                string id = data.Value<string>("id");
                YouTubeUrl.Normalize("https://youtu.be/" + id);
                double seconds = data.Value<double?>("duration") ?? 0;
                if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
                    throw new InvalidDataException("영상 길이가 올바르지 않습니다.");
                DateTime.TryParseExact(data.Value<string>("upload_date"), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
                return new VideoMetadata {
                    Id = id, Title = data.Value<string>("title") ?? "제목 없음",
                    Uploader = data.Value<string>("uploader") ?? data.Value<string>("channel") ?? "알 수 없음",
                    UploadDate = date == DateTime.MinValue ? DateTime.Today : date, Duration = TimeSpan.FromSeconds(seconds),
                    Formats = (data["formats"] as JArray ?? new JArray()).OfType<JObject>()
                        .Where(f => f.Value<string>("vcodec") != "none" && (f.Value<int?>("height") ?? 0) > 0)
                        .Select(f => new VideoFormat { Height = f.Value<int?>("height"), Fps = (int)Math.Round(f.Value<double?>("fps") ?? 30) }).ToList()
                };
            } catch (Exception ex) when (ex is Newtonsoft.Json.JsonException || ex is ArgumentException || ex is OverflowException || ex is InvalidCastException) {
                throw new InvalidDataException("영상 메타데이터가 올바르지 않습니다.", ex);
            }
        }
    }
    public sealed class VideoMetadata {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Uploader { get; set; }
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public List<VideoFormat> Formats { get; set; }
    }
    public sealed class VideoFormat {
        public int? Height { get; set; }
        public int? Fps { get; set; }
    }
}
