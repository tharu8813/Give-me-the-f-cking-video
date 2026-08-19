using System;
using System.IO;
using System.Linq;
using GMTFV.tools;

namespace GMTFV.services {
    /// <summary>
    /// yt-dlp 다운로드 옵션을 한 곳에서 검증하고 명령행으로 변환합니다.
    /// WinForms 이벤트 코드가 외부 도구의 세부 문법을 알 필요 없게 합니다.
    /// </summary>
    internal static class YtDlpDownloadCommandFactory {
        public static YtDlpDownloadCommand Create(
            VideoInfo video,
            string outputPath,
            string toolDirectory,
            string videoEncoder,
            int audioBitrate) {

            if (video == null)
                throw new ArgumentNullException(nameof(video));
            if (string.IsNullOrWhiteSpace(video.ID))
                throw new ArgumentException("영상 ID가 없습니다.", nameof(video));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentNullException(nameof(outputPath));

            bool isVideo = video.TypeSave?.IsTypeVideo ?? true;
            string extension = NormalizeExtension(video.TypeSave?.SubType, isVideo);
            string url = "https://www.youtube.com/watch?v=" + video.ID;
            string ffmpegPath = Path.Combine(toolDirectory, "ffmpeg.exe");
            string ffmpegOption = File.Exists(ffmpegPath)
                ? "--ffmpeg-location \"" + ffmpegPath + "\""
                : string.Empty;

            string formatOption;
            string conversionOption = string.Empty;

            if (isVideo) {
                bool useBestQuality = string.Equals(video.Tag as string, "USE_BEST_QUALITY", StringComparison.Ordinal);
                VideoQuality quality = video.VideoQualities?.FirstOrDefault(item => item.IsSelected);
                if (useBestQuality || quality == null) {
                    formatOption = "-f bestvideo+bestaudio/best";
                } else {
                    string height = quality.Quality?.Replace("p", string.Empty);
                    if (!int.TryParse(height, out int parsedHeight) || parsedHeight <= 0 || quality.Fps <= 0)
                        throw new ArgumentException("선택한 영상 화질 정보가 올바르지 않습니다.", nameof(video));

                    formatOption = $"-f \"bestvideo[height<={parsedHeight}][fps<={quality.Fps}]+bestaudio/best[height<={parsedHeight}]\"";
                }

                conversionOption = "--merge-output-format " + extension;
                if (extension == "mp4" || extension == "avi" || extension == "mov") {
                    conversionOption += $" --postprocessor-args \"ffmpeg:{videoEncoder} -c:a aac -b:a {audioBitrate}k\"";
                }
            } else {
                // FFmpeg 옵션을 yt-dlp의 최상위 인자로 전달하지 않고, 공식 오디오 후처리 옵션을 사용합니다.
                formatOption = "-f bestaudio/best";
                conversionOption = "--extract-audio --audio-format " + extension;
                if (extension == "mp3")
                    conversionOption += " --audio-quality 0";
            }

            // FFmpeg의 out_time 진행 정보를 stderr로 받아 병합/변환 진행률을 표시합니다.
            string progressOption = "--postprocessor-args \"ffmpeg:-progress pipe:2 -nostats\"";
            string arguments = $"{ffmpegOption} {formatOption} {conversionOption} {progressOption} --no-playlist --newline -o \"{outputPath}\" \"{url}\"".Trim();
            return new YtDlpDownloadCommand(arguments, extension);
        }

        private static string NormalizeExtension(string extension, bool isVideo) {
            string normalized = (extension ?? (isVideo ? "mp4" : "mp3")).Trim().ToLowerInvariant();
            string[] supported = isVideo ? Tol.VideoFormats : Tol.AudioFormats;
            if (!supported.Contains(normalized))
                throw new ArgumentException("지원하지 않는 저장 형식입니다: " + normalized, nameof(extension));
            return normalized;
        }
    }

    internal sealed class YtDlpDownloadCommand {
        public YtDlpDownloadCommand(string arguments, string desiredExtension) {
            Arguments = arguments;
            DesiredExtension = desiredExtension;
        }

        public string Arguments { get; }
        public string DesiredExtension { get; }
    }
}
