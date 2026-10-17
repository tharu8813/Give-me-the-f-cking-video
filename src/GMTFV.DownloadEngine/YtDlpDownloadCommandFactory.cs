using System;
using System.Collections.Generic;
using System.Linq;
using GMTFV.Core;

namespace GMTFV.DownloadEngine {
    public static class YtDlpDownloadCommandFactory {
        public const string CompletedPrefix = "GMTFV_FILE:";
        public static List<string> Create(DownloadRequest request, string outputTemplate, bool fallback = false, bool cpu = false, bool disablePlugins = false) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string url = YouTubeUrl.Normalize(request.Url);
            string extension = (request.Extension ?? "").ToLowerInvariant();
            if (!(request.IsVideo ? MediaFormats.Video : MediaFormats.Audio).Contains(extension)) throw new ArgumentException("지원하지 않는 저장 형식입니다.");
            if (request.Height <= 0 || request.Fps <= 0) throw new ArgumentException("화질 정보가 올바르지 않습니다.");
            if (request.AudioBitrate < 32 || request.AudioBitrate > 512) throw new ArgumentException("오디오 비트레이트가 올바르지 않습니다.");
            if (string.IsNullOrWhiteSpace(outputTemplate)) throw new ArgumentException("출력 경로가 없습니다.");
            var args = new List<string> { "--verbose", "--newline", "--no-simulate", "--no-quiet", "--no-overwrites", "--continue", "--windows-filenames", "--no-mtime",
                "--print", "after_move:" + CompletedPrefix + "%(filepath)j", "--progress", "--progress-template",
                "download:GMTFV_PROGRESS:%(progress.status)s|%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s",
                "--progress-template", "postprocess:GMTFV_POSTPROCESS:%(progress.postprocessor)s|%(progress.status)s" };
            if (disablePlugins) args.Add("--no-plugin-dirs");
            if (request.IsVideo) {
                string comparison = request.AllowQualityFallback ? "<=" : "=";
                string filter = request.Height.HasValue ? "[height" + comparison + request.Height.Value + "]" : "";
                // A recovery attempt may relax FPS, but never downloads above the requested resolution.
                if (request.Fps.HasValue && !(fallback && request.AllowQualityFallback))
                    filter += request.AllowQualityFallback ? "[fps<=" + request.Fps.Value + "]" : "[fps>" + (request.Fps.Value - 1) + "][fps<=" + request.Fps.Value + "]";
                string video = "bestvideo" + filter;
                string audio = "bestaudio";
                if (extension == "webm") { video += "[ext=webm]"; audio += "[ext=webm]"; }
                string format = video + "+" + audio + "/best" + filter + (extension == "webm" ? "[ext=webm]" : "");
                args.AddRange(new[] { "-f", format });
                if (extension == "avi" || extension == "flv") {
                    string encoder = cpu ? "libx264" : request.VideoEncoder;
                    if (!new[] { "libx264", "h264_nvenc", "h264_amf", "h264_qsv" }.Contains(encoder)) throw new ArgumentException("지원하지 않는 인코더입니다.");
                    args.AddRange(new[] { "--recode-video", extension, "--postprocessor-args",
                        "VideoConvertor+ffmpeg_o:-c:v " + encoder + (extension == "avi" ? " -vtag H264" : "") + " -c:a " + (extension == "avi" ? "libmp3lame" : "aac") + " -b:a " + request.AudioBitrate + "k" });
                } else {
                    args.AddRange(new[] { "--merge-output-format", extension, "--remux-video", extension });
                    if (extension == "mp4" || extension == "mov") args.AddRange(new[] { "-S", "res,fps,vcodec:h264,acodec:aac" });
                }
            } else {
                args.AddRange(new[] { "-f", "bestaudio/best", "--extract-audio", "--audio-format", extension == "ogg" ? "vorbis" : extension,
                    "--audio-quality", request.AudioBitrate + "K" });
                // yt-dlp can retain M4A when its AAC stream already matches; enforce the requested ADTS container.
                if (extension == "aac") args.AddRange(new[] { "--remux-video", "aac" });
            }
            args.AddRange(new[] { "-o", outputTemplate, "--", url });
            return args;
        }
    }
}
