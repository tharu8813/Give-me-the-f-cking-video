using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;
using GMTFV.DownloadEngine;
using Newtonsoft.Json;

internal static partial class Program {
    private static string ProbeJson(string codec, string container, bool video = false) => JsonConvert.SerializeObject(new {
        streams = video ? new[] { new { codec_type = "audio", codec_name = codec }, new { codec_type = "video", codec_name = "h264" } } : new[] { new { codec_type = "audio", codec_name = codec } },
        format = new { duration = "10.5", format_name = container }
    });
    private static async Task CheckStabilityAsync() {
        var runner = new ProcessRunner();
        await Test("streaming stdout over 16MB remains bounded and succeeds", async () => {
            int lines = 0;
            var result = await runner.RunAsync(Self, "--fake flood", TimeSpan.FromSeconds(30), CancellationToken.None, _ => lines++);
            result.EnsureSuccess(); Check(result.Output.Length <= 65536 && result.Output.EndsWith("OUT-END\r\n")); Equal(18001, lines);
        });
        await Test("unbroken stdout line stays bounded in streaming mode", async () => {
            int longest = 0;
            var result = await runner.RunAsync(Self, "--fake longline", TimeSpan.FromSeconds(30), CancellationToken.None, line => longest = Math.Max(longest, line.Length));
            result.EnsureSuccess(); Check(longest <= 65536 && result.Output.Contains("OUT-END"));
        });
        await Test("metadata output keeps strict limit instead of parsing truncated JSON", () => ThrowsAsync<InvalidDataException>(() => runner.RunAsync(Self, "--fake longline", TimeSpan.FromSeconds(30), CancellationToken.None)));
        await Test("callback exception propagates after pipes drain", () => ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(Self, "--fake streams", TimeSpan.FromSeconds(30), CancellationToken.None, _ => throw new Exception("callback"))));
        var cases = new[] {
            ("aac", "mov,mp4,m4a", "m4a", "m4a", AudioProcessing.DirectSave),
            ("aac", "aac", "aac", "m4a", AudioProcessing.StreamCopy),
            ("aac", "mov,mp4,m4a", "m4a", "aac", AudioProcessing.StreamCopy),
            ("aac", "aac", "aac", "aac", AudioProcessing.DirectSave),
            ("opus", "matroska,webm", "webm", "opus", AudioProcessing.StreamCopy),
            ("opus", "ogg", "opus", "opus", AudioProcessing.DirectSave),
            ("opus", "ogg", "ogg", "ogg", AudioProcessing.Transcode),
            ("vorbis", "ogg", "ogg", "ogg", AudioProcessing.DirectSave),
            ("vorbis", "matroska,webm", "webm", "ogg", AudioProcessing.StreamCopy),
            ("aac", "mp3", "mp3", "mp3", AudioProcessing.Transcode),
            ("mp3", "mp3", "mp3", "mp3", AudioProcessing.DirectSave),
            ("mp3", "mov,mp4,m4a", "m4a", "m4a", AudioProcessing.Transcode),
            ("aac", "mov,mp4,m4a", "m4a", "original", AudioProcessing.DirectSave),
            ("opus", "matroska,webm", "webm", "original", AudioProcessing.DirectSave),
            ("flac", "flac", "flac", "flac", AudioProcessing.DirectSave),
            ("aac", "mov,mp4,m4a", "m4a", "flac", AudioProcessing.Transcode),
            ("pcm_s24le", "wav", "wav", "wav", AudioProcessing.DirectSave),
            ("aac", "mov,mp4,m4a", "m4a", "wav", AudioProcessing.Transcode),
            ("opus", "matroska,webm", "opus", "opus", AudioProcessing.StreamCopy)
        };
        foreach (var item in cases) await Test("audio codec policy " + string.Join("/", item.Item1, item.Item2, item.Item3, item.Item4), () => {
            Equal(item.Item5, AudioProcessor.Decide(MediaProbe.Parse(ProbeJson(item.Item1, item.Item2)), item.Item3, item.Item4)); return Task.CompletedTask;
        });
        await Test("original rejects multiplexed video instead of saving video as audio", () => Throws<InvalidDataException>(() => AudioProcessor.Decide(MediaProbe.Parse(ProbeJson("aac", "mov,mp4", true)), "mp4", "original")));
        await Test("audio extraction stream copies codec from multiplexed video", () => { Equal(AudioProcessing.StreamCopy, AudioProcessor.Decide(MediaProbe.Parse(ProbeJson("aac", "mov,mp4", true)), "mp4", "m4a")); return Task.CompletedTask; });
        await Test("ffprobe rejects empty or invalid media", async () => {
            await Throws<InvalidDataException>(() => MediaProbe.Parse("{}"));
            await Throws<InvalidDataException>(() => MediaProbe.Parse("broken"));
            await Throws<InvalidDataException>(() => MediaProbe.Parse(ProbeJson("aac", "mov").Replace("10.5", "NaN")));
        });
        foreach (string locale in new[] { "ko-KR", "de-DE", "en-US" }) await Test("measured progress culture " + locale, () => {
            var previous = CultureInfo.CurrentCulture; CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            try {
                Equal(50, DownloadService.ParseProgress("GMTFV_PROGRESS:downloading|50.5%|1MiB/s|10|none|opus|251").Percent);
                Equal(DownloadStage.AudioDownload, DownloadService.ParseProgress("GMTFV_PROGRESS:downloading|50.5%|1MiB/s|10|none|opus|251").Stage);
                Equal(50, AudioProcessor.ParseProgress("out_time_us=5000000", 10, TimeSpan.FromSeconds(2), DownloadStage.EncodeAudio, "audio").Percent);
                Equal(99, AudioProcessor.ParseProgress("out_time_us=10000000", 10, TimeSpan.FromSeconds(2), DownloadStage.EncodeAudio, "audio").Percent);
                Check(AudioProcessor.ParseProgress("out_time_us=NaN", 10, TimeSpan.Zero, DownloadStage.EncodeAudio, "audio") == null);
            } finally { CultureInfo.CurrentCulture = previous; }
            return Task.CompletedTask;
        });
        await Test("postprocess distinguishes actual stage and ignores finished notifications", () => {
            Equal(DownloadStage.Remux, DownloadService.ParseProgress("GMTFV_POSTPROCESS:VideoRemuxer|started").Stage);
            Equal(DownloadStage.EncodeVideo, DownloadService.ParseProgress("GMTFV_POSTPROCESS:VideoConvertor|processing").Stage);
            Check(DownloadService.ParseProgress("GMTFV_POSTPROCESS:Merger|finished") == null);
            Check(DownloadService.ParseProgress("GMTFV_POSTPROCESS:Merger|started").Percent == null);
            Equal(DownloadStage.Save, DownloadService.ParseProgress("GMTFV_POSTPROCESS:MoveFiles|started").Stage);
            Check(DownloadService.ParseProgress("GMTFV_POSTPROCESS:Metadata|started") == null);
            return Task.CompletedTask;
        });
        await Test("original output adopts real extension and preserves collision", async () => {
            string old = Path.Combine(Root, "original.m4a"); WriteMp4(old);
            var fake = new FakeRunner { OutputExtension = "m4a", ProbeVideo = false };
            var request = Request("original.original"); request.IsVideo = false; request.Extension = "original";
            string output = await Service(fake).DownloadAsync(request, null, CancellationToken.None);
            Check(output.EndsWith("original (1).m4a") && File.Exists(old));
        });
        await Test("fake media header with unplayable streams never completes", async () => {
            var fake = new FakeRunner { ProbeOutput = "{}" }; var states = new System.Collections.Generic.List<DownloadState>();
            await ThrowsAsync<InvalidOperationException>(() => Service(fake).DownloadAsync(Request("invalid-streams.mp4"), new Capture(p => states.Add(p.State)), CancellationToken.None));
            Check(!states.Contains(DownloadState.Completed) && !File.Exists(Path.Combine(Root, "invalid-streams.mp4")));
        });
    }
    private static int MediaDownload(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string mode = args.Length == 7 ? args[6] : "";
        using var cancellation = new CancellationTokenSource();
        try {
            string tools = Path.GetFullPath(args[1]);
            var request = new DownloadRequest { Url = "https://youtu.be/" + Id, Extension = args[2], IsVideo = args[3] == "video", OutputPath = Path.GetFullPath(args[5]) };
            var service = new DownloadService(tools, new FixtureRunner(Path.GetFullPath(args[4]), mode), new DiagnosticLog(Path.Combine(Path.GetDirectoryName(request.OutputPath), "logs")));
            string result = service.DownloadAsync(request, new Capture(p => {
                Console.Error.WriteLine($"STAGE {p.State}/{p.Stage} percent={p.Percent} {p.Message}");
                if (mode == "cancel-audio" && p.Stage == DownloadStage.EncodeAudio && p.Percent.HasValue) cancellation.Cancel();
                if (mode == "cancel-merge" && p.Stage == DownloadStage.Merge) cancellation.CancelAfter(150);
            }), cancellation.Token).GetAwaiter().GetResult();
            if (mode.StartsWith("cancel-")) throw new Exception("expected cancellation before completion");
            Console.Write(JsonConvert.SerializeObject(result)); return 0;
        } catch (OperationCanceledException) when (mode.StartsWith("cancel-")) {
            Check(!File.Exists(args[5])); Console.Write(JsonConvert.SerializeObject("cancelled")); return 0;
        } catch (Exception ex) { Console.Error.WriteLine(ex); for (var error = ex; error != null; error = error.InnerException) if (error is ToolExecutionException tool) Console.Error.WriteLine(tool.Error); return 1; }
    }
    private sealed class FixtureRunner(string fixture, string mode) : IProcessRunner {
        private readonly ProcessRunner runner = new();
        public Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken token, Action<string> output = null, Action<string> error = null) {
            string[] args = Split(arguments);
            if (Path.GetFileName(executable) == "yt-dlp.exe" && args.Contains("-o")) {
                arguments = CommandLine.Join(args.Take(args.Length - 2).Concat(new[] { "--load-info-json", fixture }));
                if (mode == "cancel-merge") arguments += " " + CommandLine.Join(new[] { "--postprocessor-args", "Merger+ffmpeg_i:-re" });
            }
            if (Path.GetFileName(executable) == "ffmpeg.exe" && args.Contains("-progress")) {
                if (mode == "cancel-audio") arguments = CommandLine.Join(args.Take(Array.IndexOf(args, "-i")).Concat(new[] { "-re" }).Concat(args.Skip(Array.IndexOf(args, "-i"))));
                if (mode == "fail-audio") { args[Array.IndexOf(args, "-c:a") + 1] = "gmtfv_missing_encoder"; arguments = CommandLine.Join(args); }
            }
            return runner.RunAsync(executable, arguments, timeout, token, output, error);
        }
    }
}
