using GMTFV.tools;
using System;
using System.Linq;
using GMTFV.Core;

namespace GMTFV.services {
    internal static class YtDlpDownloadCommandFactory {
        public static DownloadRequest Create(VideoInfo video, string outputPath, string videoEncoder, int audioBitrate, bool allowQualityFallback) {
            if (video == null) throw new ArgumentNullException(nameof(video));
            VideoQuality quality = video.Tag == "USE_BEST_QUALITY" ? null : video.VideoQualities?.FirstOrDefault(item => item.IsSelected);
            int? height = null;
            if (quality != null) {
                if (!int.TryParse(quality.Quality?.TrimEnd('p'), out int parsed) || parsed <= 0) throw new ArgumentException("선택한 화질이 올바르지 않습니다.");
                height = parsed;
            }
            return new DownloadRequest {
                Url = "https://www.youtube.com/watch?v=" + video.ID, OutputPath = outputPath,
                IsVideo = video.TypeSave?.IsTypeVideo ?? true, Extension = video.TypeSave?.SubType ?? "mp4",
                Height = height, Fps = quality?.Fps, VideoEncoder = videoEncoder.Replace("-c:v ", ""),
                AudioBitrate = audioBitrate, AllowQualityFallback = allowQualityFallback
            };
        }
    }
}
