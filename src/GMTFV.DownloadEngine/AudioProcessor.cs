using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;
using Newtonsoft.Json.Linq;

namespace GMTFV.DownloadEngine {
    public enum AudioProcessing { DirectSave, StreamCopy, Transcode }

    public sealed class MediaProbe {
        public string AudioCodec { get; private set; }
        public string Container { get; private set; }
        public bool HasVideo { get; private set; }
        public double Duration { get; private set; }
        public static MediaProbe Parse(string json) {
            try {
                var data = JObject.Parse(json);
                var streams = (data["streams"] as JArray ?? new JArray()).OfType<JObject>().ToArray();
                var audio = streams.FirstOrDefault(s => s.Value<string>("codec_type") == "audio");
                double.TryParse((string)data["format"]?["duration"], NumberStyles.Float, CultureInfo.InvariantCulture, out double duration);
                if (!streams.Any(s => s.Value<string>("codec_type") == "video" || s.Value<string>("codec_type") == "audio") ||
                    double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0 || duration > TimeSpan.MaxValue.TotalSeconds)
                    throw new InvalidDataException("재생 가능한 스트림이나 미디어 길이를 확인할 수 없습니다.");
                return new MediaProbe { AudioCodec = audio?.Value<string>("codec_name"), Container = (string)data["format"]?["format_name"] ?? "",
                    HasVideo = streams.Any(s => s.Value<string>("codec_type") == "video" && s["disposition"]?.Value<int?>("attached_pic") != 1), Duration = duration };
            } catch (Exception ex) when (ex is Newtonsoft.Json.JsonException || ex is FormatException || ex is InvalidCastException || ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException) {
                throw new InvalidDataException("FFprobe 미디어 정보가 올바르지 않습니다.", ex);
            }
        }
        public static async Task<MediaProbe> ReadAsync(string directory, string path, IProcessRunner runner, CancellationToken token) {
            var result = await runner.RunAsync(Path.Combine(directory, "ffprobe.exe"), CommandLine.Join(new[] {
                "-v", "error", "-show_streams", "-show_format", "-of", "json", Path.GetFullPath(path) }), TimeSpan.FromSeconds(60), token).ConfigureAwait(false);
            if (result.ExitCode != 0) throw new InvalidDataException("FFprobe가 미디어를 확인하지 못했습니다: " + result.Error);
            return Parse(result.Output);
        }
        public bool MatchesContainer(string extension) {
            string name = extension == "aac" ? "aac" : extension == "m4a" || extension == "mp4" || extension == "mov" ? "mov" :
                extension == "opus" || extension == "ogg" ? "ogg" : extension == "mkv" || extension == "mka" ? "matroska" :
                extension == "weba" ? "webm" : extension;
            return Container.Split(',').Contains(name);
        }
    }

    public static class AudioProcessor {
        public static bool CompatibleCodec(string codec, string extension) {
            switch (extension) {
                case "m4a": case "aac": return codec == "aac";
                case "ogg": return codec == "vorbis";
                case "wav": return codec != null && codec.StartsWith("pcm_", StringComparison.Ordinal);
                default: return codec == extension;
            }
        }
        public static AudioProcessing Decide(MediaProbe source, string sourceExtension, string requested) {
            if (string.IsNullOrEmpty(source.AudioCodec)) throw new InvalidDataException("오디오 스트림이 없습니다.");
            if (requested == "original") {
                if (source.HasVideo) throw new InvalidDataException("원본 오디오는 오디오 전용 스트림이 필요합니다.");
                return AudioProcessing.DirectSave;
            }
            if (!MediaFormats.Audio.Contains(requested)) throw new ArgumentException("지원하지 않는 오디오 형식입니다.");
            if (!CompatibleCodec(source.AudioCodec, requested)) return AudioProcessing.Transcode;
            return !source.HasVideo && sourceExtension == requested && source.MatchesContainer(requested) ? AudioProcessing.DirectSave : AudioProcessing.StreamCopy;
        }
        public static async Task<string> ProcessAsync(string path, DownloadRequest request, string directory, IProcessRunner runner,
            IProgress<DownloadProgress> progress, CancellationToken token) {
            var source = await MediaProbe.ReadAsync(directory, path, runner, token).ConfigureAwait(false);
            var policy = Decide(source, Path.GetExtension(path).TrimStart('.').ToLowerInvariant(), request.Extension);
            if (policy == AudioProcessing.DirectSave) return path;
            string target = Path.Combine(Path.GetDirectoryName(path), "processed." + request.Extension);
            var stage = policy == AudioProcessing.StreamCopy ? DownloadStage.Remux : DownloadStage.EncodeAudio;
            string label = policy == AudioProcessing.StreamCopy ? "오디오 컨테이너 변경 · 재인코딩 없음" : "오디오 인코딩 · " + request.Extension.ToUpperInvariant();
            progress?.Report(new DownloadProgress { State = DownloadState.Converting, Stage = stage, Message = label });
            var args = new List<string> { "-hide_banner", "-nostdin", "-n", "-i", Path.GetFullPath(path), "-map", "0:a:0", "-vn", "-c:a" };
            string codec = request.Extension == "mp3" ? "libmp3lame" : request.Extension == "opus" ? "libopus" :
                request.Extension == "ogg" ? "libvorbis" : request.Extension == "wav" ? "pcm_s16le" : request.Extension == "m4a" ? "aac" : request.Extension;
            args.Add(policy == AudioProcessing.StreamCopy ? "copy" : codec);
            if (policy == AudioProcessing.Transcode && request.Extension != "wav" && request.Extension != "flac")
                args.AddRange(new[] { "-b:a", request.AudioBitrate.ToString(CultureInfo.InvariantCulture) + "k" });
            args.AddRange(new[] { "-progress", "pipe:1", "-nostats", target });
            var clock = Stopwatch.StartNew();
            TimeSpan last = TimeSpan.FromSeconds(-1);
            var result = await runner.RunAsync(Path.Combine(directory, "ffmpeg.exe"), CommandLine.Join(args), TimeSpan.FromHours(6), token, line => {
                var update = ParseProgress(line, source.Duration, clock.Elapsed, stage, label);
                if (update != null && clock.Elapsed - last >= TimeSpan.FromMilliseconds(200)) { last = clock.Elapsed; progress?.Report(update); }
            }).ConfigureAwait(false);
            if (result.ExitCode != 0) throw new ToolExecutionException(result.ExitCode, "FFmpeg audio processing: " + result.Error);
            token.ThrowIfCancellationRequested();
            return target;
        }
        public static DownloadProgress ParseProgress(string line, double duration, TimeSpan elapsed, DownloadStage stage, string label) {
            if (!line.StartsWith("out_time_us=", StringComparison.Ordinal) || !double.TryParse(line.Substring(12), NumberStyles.Float, CultureInfo.InvariantCulture, out double micros) ||
                micros < 0 || double.IsNaN(micros) || double.IsInfinity(micros) || duration <= 0 || double.IsNaN(duration) || double.IsInfinity(duration)) return null;
            double ratio = Math.Min(1, micros / 1000000 / duration);
            int percent = (int)Math.Min(99, ratio * 100);
            string eta = ratio > 0 && ratio < 1 ? " · 약 " + Math.Ceiling(elapsed.TotalSeconds * (1 - ratio) / ratio).ToString(CultureInfo.InvariantCulture) + "초 남음" : "";
            return new DownloadProgress { State = DownloadState.Converting, Stage = stage, Percent = percent,
                Message = label + " · " + percent + "% · " + (int)elapsed.TotalSeconds + "초 경과" + eta };
        }
    }
}
