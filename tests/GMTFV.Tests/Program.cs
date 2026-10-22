using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GMTFV.Core;
using GMTFV.DownloadEngine;
using Newtonsoft.Json;

internal static partial class Program {
    private static int passed, failed;
    private static readonly string Self = Environment.ProcessPath;
    private static readonly string Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gmtfv-tests-" + Guid.NewGuid().ToString("N"));
    private const string Id = "BaW_jenozKc";
    private static int Main(string[] args) {
        if (args.Length > 0 && args[0] == "--fake") return FakeProcess(args.Skip(1).ToArray());
        if (args.Length == 5 && args[0] == "--emit-command") {
            var request = new DownloadRequest { Url = "https://youtu.be/" + Id, Extension = args[2], IsVideo = args[3] == "video" };
            Console.Write(JsonConvert.SerializeObject(YtDlpClient.CommonArguments(args[1]).Concat(YtDlpDownloadCommandFactory.Create(request, args[4]))));
            return 0;
        }
        if (args.Length is 6 or 7 && args[0] == "--media-download") return MediaDownload(args);
        Directory.CreateDirectory(Root);
        try { Run().GetAwaiter().GetResult(); }
        finally { Directory.Delete(Root, true); }
        Console.WriteLine($"RESULT: {passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }

    private static async Task Run() {
        await CheckStabilityAsync();
        foreach (string url in new[] { "https://youtu.be/" + Id, "https://www.youtube.com/watch?v=" + Id + "&list=xyz", "https://m.youtube.com/shorts/" + Id, "https://youtube.com/live/" + Id, "http://music.youtube.com/watch?v=" + Id })
            await Test("normalize " + url, () => { Equal("https://www.youtube.com/watch?v=" + Id, YouTubeUrl.Normalize(url)); return Task.CompletedTask; });
        foreach (string url in new[] { "https://evil.com/youtube.com/watch?v=" + Id, "https://youtube.com.evil.com/watch?v=" + Id, "file:///youtube.com/watch?v=" + Id, "https://youtube.com@evil.com/watch?v=" + Id, "https://youtu.be/" + Id + "EXTRA", "https://youtube.com:8443/watch?v=" + Id, "https://youtu.be/short", "" })
            await Test("reject URL " + url, () => Throws<ArgumentException>(() => YouTubeUrl.Normalize(url)));
        await Test("normalized duplicate", () => { Equal(YouTubeUrl.Normalize("https://youtu.be/" + Id + "?si=secret"), YouTubeUrl.Normalize("https://youtube.com/watch?v=" + Id)); return Task.CompletedTask; });
        await Test("template extension once", () => { Equal("title.mp4", WindowsFileName.Build("title", "author", Id, "mp4", "%title%.%ext%", 1)); Equal("001_title.mp4", WindowsFileName.Build("title", "author", Id, "mp4", "%num3%_%title%", 1)); return Task.CompletedTask; });
        await Test("Windows names", () => { Equal("_CON", WindowsFileName.Sanitize("CON. ")); Equal("_aux.txt", WindowsFileName.Sanitize("aux.txt")); Equal("a_b_c_", WindowsFileName.Sanitize("a/b:c?")); Equal("unnamed", WindowsFileName.Sanitize(" ... ")); return Task.CompletedTask; });
        await Test("long path budget", () => { Check(WindowsFileName.FitPath(Path.Combine(Root, new string('a', 240) + ".mp4")).Length <= 230); return Throws<PathTooLongException>(() => WindowsFileName.FitPath("C:\\" + new string('a', 225) + "\\file.mp4")); });

        await Test("video remux no encode", () => { var args = YtDlpDownloadCommandFactory.Create(Request(), "media.%(ext)s"); Check(args.Contains("--remux-video")); Check(!args.Contains("--postprocessor-args")); Check(args.Contains("after_move:GMTFV_FILE:%(filepath)j")); Check(args.Contains("--no-simulate")); Check(args.Contains("res,fps,vcodec:h264,acodec:aac")); return Task.CompletedTask; });
        await Test("quality bounds and opt in fallback", () => { var request = Request(); request.Height = 720; request.Fps = 30; var args = YtDlpDownloadCommandFactory.Create(request, "out"); Check(args[args.IndexOf("-f") + 1].Contains("height<=720")); Check(!string.Join(" ", YtDlpDownloadCommandFactory.Create(request, "out", true)).Contains("fps<=")); request.AllowQualityFallback = false; Check(string.Join(" ", YtDlpDownloadCommandFactory.Create(request, "out", true)).Contains("height=720")); return Task.CompletedTask; });
        await Test("webm selects compatible streams", () => { var request = Request(); request.Extension = "webm"; Check(string.Join(" ", YtDlpDownloadCommandFactory.Create(request, "out")).Contains("bestvideo[ext=webm]+bestaudio[ext=webm]")); return Task.CompletedTask; });
        await Test("FFmpeg single scoped encode with CPU fallback", () => { var request = Request(); request.Extension = "avi"; request.VideoEncoder = "h264_nvenc"; var args = YtDlpDownloadCommandFactory.Create(request, "out"); Equal(1, args.Count(a => a == "--postprocessor-args")); Check(args.Contains("VideoConvertor+ffmpeg_o:-c:v h264_nvenc -vtag H264 -c:a libmp3lame -b:a 192k")); Check(string.Join(" ", YtDlpDownloadCommandFactory.Create(request, "out", cpu: true)).Contains("-c:v libx264")); return Task.CompletedTask; });
        foreach (string extension in MediaFormats.Audio)
            await Test("audio source command " + extension, () => { var request = Request(); request.IsVideo = false; request.Extension = extension; var args = YtDlpDownloadCommandFactory.Create(request, "out"); Check(!args.Contains("--extract-audio") && !args.Contains("--audio-quality") && !args.Contains("--merge-output-format")); Equal(extension == "original" ? "bestaudio" : "bestaudio/best", args[args.IndexOf("-f") + 1]); return Task.CompletedTask; });
        await Test("reject injected codec/format", async () => { var request = Request(); request.Extension = "mp4 --exec evil"; await Throws<ArgumentException>(() => YtDlpDownloadCommandFactory.Create(request, "out")); request.Extension = "avi"; request.VideoEncoder = "libx264 -exec evil"; await Throws<ArgumentException>(() => YtDlpDownloadCommandFactory.Create(request, "out")); });

        var errors = new Dictionary<string, DownloadError> {
            ["HTTP Error 403: Forbidden"] = DownloadError.Forbidden, ["HTTP Error 429: Too Many Requests"] = DownloadError.RateLimited,
            ["connection reset by peer"] = DownloadError.Network, ["connection timed out"] = DownloadError.Timeout,
            ["PO Token provider failed"] = DownloadError.PoToken, ["No supported JavaScript runtime"] = DownloadError.Client,
            ["Requested format is not available"] = DownloadError.Format, ["ffmpeg exited with code 1"] = DownloadError.FFmpeg,
            ["Permission denied"] = DownloadError.Permission, ["No space left on device"] = DownloadError.DiskFull,
            ["Private video. Sign in"] = DownloadError.Unavailable, ["unexpected mystery"] = DownloadError.Unknown
        };
        foreach (var pair in errors) await Test("classify " + pair.Value, () => { Equal(pair.Value, RecoveryPolicy.Classify(new ToolExecutionException(1, pair.Key))); return Task.CompletedTask; });
        await Test("bounded retry policy", () => { foreach (DownloadError error in Enum.GetValues(typeof(DownloadError))) Check(!RecoveryPolicy.ShouldRetry(error, 3, true, true)); Check(!RecoveryPolicy.ShouldRetry(DownloadError.RateLimited, 1, true, true)); Check(!RecoveryPolicy.ShouldRetry(DownloadError.Format, 1, false, false)); Check(RecoveryPolicy.ShouldRetry(DownloadError.Network, 2, false, false)); Check(RecoveryPolicy.Delay(2) > RecoveryPolicy.Delay(1)); return Task.CompletedTask; });
        await Test("metadata decimal FPS and unknown fields", () => { var m = VideoMetadataService.Parse("{\"id\":\"" + Id + "\",\"duration\":12.5,\"formats\":[{\"height\":1080,\"fps\":59.94,\"vcodec\":\"vp9\"},{\"height\":null,\"vcodec\":\"none\"}],\"extra\":1}"); Equal(60, m.Formats.Single().Fps.Value); Equal(12.5, m.Duration.TotalSeconds); return Task.CompletedTask; });
        await Test("reject invalid metadata ID", () => Throws<InvalidDataException>(() => VideoMetadataService.Parse("{\"title\":\"missing id\"}")));
        await Test("reject broken JSON", () => Throws<InvalidDataException>(() => VideoMetadataService.Parse("{broken")));
        await Test("progress never completes before validation", () => { Equal(99, DownloadService.ParseProgress("GMTFV_PROGRESS:finished|100.0%|1MiB/s|0").Percent); Check(DownloadService.ParseProgress("[Merger] merging").Percent == null); Check(DownloadService.ParseProgress("has already been downloaded") == null); return Task.CompletedTask; });
        await Test("secret redaction", () => { string value = DiagnosticLog.Redact("https://video.test/path?token=secret\nAuthorization: Bearer credential\npo_token=abc\n" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)); Check(!value.Contains("secret") && !value.Contains("credential") && !value.Contains("abc")); Check(value.Contains("[USER]")); return Task.CompletedTask; });
        await Test("bounded log rotation", () => { string directory = Path.Combine(Root, "rotate"); Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "download.log"), new string('x', 2 * 1024 * 1024 + 1)); new DiagnosticLog(directory).Write("id", "done"); Check(File.Exists(Path.Combine(directory, "download.log.1"))); Check(new FileInfo(Path.Combine(directory, "download.log")).Length < 1024); return Task.CompletedTask; });

        await Test("queue exact concurrency and unique slots", async () => {
            using (var queue = new DownloadQueueService(2)) {
                int active = 0, peak = 0; var leased = new HashSet<int>();
                await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ => {
                    int slot = await queue.AcquireSlotAsync(CancellationToken.None);
                    lock (leased) { Check(leased.Add(slot)); active++; peak = Math.Max(active, peak); }
                    await Task.Delay(15);
                    lock (leased) { active--; leased.Remove(slot); }
                    queue.ReleaseSlot(slot);
                })); Equal(2, peak);
            }
        });
        await Test("waiting cancellation doesn't release a slot", async () => {
            using (var queue = new DownloadQueueService(1)) using (var cts = new CancellationTokenSource()) {
                int slot = await queue.AcquireSlotAsync(CancellationToken.None);
                Task<int> waiting = queue.AcquireSlotAsync(cts.Token); cts.Cancel(); await ThrowsAsync<OperationCanceledException>(async () => await waiting);
                queue.ReleaseSlot(slot); queue.ReleaseSlot(slot); int next = await queue.AcquireSlotAsync(CancellationToken.None); Equal(slot, next); queue.ReleaseSlot(next);
            }
        });
        await Test("dispose queue with waiter", async () => { var queue = new DownloadQueueService(1); int slot = await queue.AcquireSlotAsync(CancellationToken.None); Task<int> wait = queue.AcquireSlotAsync(CancellationToken.None); queue.Dispose(); await ThrowsAsync<OperationCanceledException>(async () => await wait); queue.ReleaseSlot(slot); queue.Dispose(); });

        var runner = new ProcessRunner();
        await Test("Windows argument round trip", async () => { string[] values = { "", "space name", "C:\\ends with slash\\", "quote\"here", "한글 & | % ! $(literal)", "\\\\\"" }; var result = await runner.RunAsync(Self, CommandLine.Join(new[] { "--fake", "echo" }.Concat(values)), TimeSpan.FromSeconds(10), CancellationToken.None); result.EnsureSuccess(); Check(values.SequenceEqual(JsonConvert.DeserializeObject<string[]>(result.Output))); });
        await Test("stdout/stderr simultaneous drain", async () => { var result = await runner.RunAsync(Self, "--fake streams", TimeSpan.FromSeconds(10), CancellationToken.None); result.EnsureSuccess(); Check(result.Output.Contains("OUT-END")); Check(result.Error.Contains("ERR-END")); Check(result.Error.Length <= 65536); });
        await Test("bad exit code stays failed", async () => { var result = await runner.RunAsync(Self, "--fake fail", TimeSpan.FromSeconds(10), CancellationToken.None); Equal(7, result.ExitCode); await Throws<ToolExecutionException>(() => result.EnsureSuccess()); });
        await Test("process timeout", () => ThrowsAsync<TimeoutException>(() => runner.RunAsync(Self, "--fake sleep", TimeSpan.FromMilliseconds(250), CancellationToken.None)));
        await Test("active process cancellation", async () => { using (var cts = new CancellationTokenSource(250)) await ThrowsAsync<OperationCanceledException>(() => runner.RunAsync(Self, "--fake sleep", TimeSpan.FromSeconds(10), cts.Token)); });
        await Test("pre cancelled never starts", async () => { using (var cts = new CancellationTokenSource()) { cts.Cancel(); await ThrowsAsync<OperationCanceledException>(() => runner.RunAsync("missing.exe", "", TimeSpan.FromSeconds(10), cts.Token)); } });
        await Test("missing tool", () => ThrowsAsync<FileNotFoundException>(() => runner.RunAsync(Path.Combine(Root, "missing.exe"), "", TimeSpan.FromSeconds(1), CancellationToken.None)));
        await Test("cancel kills owned child only", async () => {
            int child = 0;
            using (var unrelated = Process.Start(new ProcessStartInfo(Self, "--fake sleep") { UseShellExecute = false, CreateNoWindow = true }))
            using (var cts = new CancellationTokenSource()) {
                await ThrowsAsync<OperationCanceledException>(() => runner.RunAsync(Self, "--fake child", TimeSpan.FromSeconds(10), cts.Token,
                    line => { if (int.TryParse(line, out child)) cts.Cancel(); }));
                Check(child > 0);
                try { using (var process = Process.GetProcessById(child)) Check(process.WaitForExit(3000)); } catch (ArgumentException) { }
                Check(!unrelated.HasExited); unrelated.Kill(); unrelated.WaitForExit();
            }
        });

        await Test("engine success validated before complete", async () => {
            var fake = new FakeRunner(); var states = new List<DownloadState>();
            string result = await Service(fake).DownloadAsync(Request("success.mp4"), new Capture(p => states.Add(p.State)), CancellationToken.None);
            Check(File.Exists(result)); Check(states.IndexOf(DownloadState.Validating) < states.IndexOf(DownloadState.Completed)); Equal(1, fake.DownloadCalls);
        });
        await Test("old same name cannot prove success", async () => { string existing = Path.Combine(Root, "old.mp4"); WriteMp4(existing); var fake = new FakeRunner { OmitOutput = true }; await ThrowsAsync<InvalidOperationException>(() => Service(fake).DownloadAsync(Request("old.mp4"), null, CancellationToken.None)); Check(File.Exists(existing)); });
        await Test("nonzero exit with file never completes", async () => { var fake = new FakeRunner { Failures = 10, ErrorText = "Private video" }; await ThrowsAsync<InvalidOperationException>(() => Service(fake).DownloadAsync(Request("bad-exit.mp4"), null, CancellationToken.None)); Equal(1, fake.DownloadCalls); Check(!File.Exists(Path.Combine(Root, "bad-exit.mp4"))); });
        await Test("network retries capped at three", async () => { var fake = new FakeRunner { Failures = 9 }; await ThrowsAsync<InvalidOperationException>(() => Service(fake).DownloadAsync(Request("network.mp4"), null, CancellationToken.None)); Equal(3, fake.DownloadCalls); });
        await Test("rate limiting no app retries", async () => { var fake = new FakeRunner { Failures = 9, ErrorText = "HTTP Error 429" }; await ThrowsAsync<InvalidOperationException>(() => Service(fake).DownloadAsync(Request("429.mp4"), null, CancellationToken.None)); Equal(1, fake.DownloadCalls); });
        await Test("cancel during retry wait", async () => { var fake = new FakeRunner { Failures = 9 }; using (var cts = new CancellationTokenSource()) { var capture = new Capture(p => { if (p.State == DownloadState.Retrying) cts.Cancel(); }); await ThrowsAsync<OperationCanceledException>(() => Service(fake).DownloadAsync(Request("retry-cancel.mp4"), capture, cts.Token)); Equal(1, fake.DownloadCalls); } });
        await Test("file collision preserves both", async () => { string target = Path.Combine(Root, "collision.mp4"); WriteMp4(target); byte[] before = File.ReadAllBytes(target); string result = await Service(new FakeRunner()).DownloadAsync(Request("collision.mp4"), null, CancellationToken.None); Check(result.EndsWith("collision (1).mp4")); Check(before.SequenceEqual(File.ReadAllBytes(target))); });
        await Test("parallel publication no overwrite", async () => { string[] results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Service(new FakeRunner()).DownloadAsync(Request("parallel.mp4"), null, CancellationToken.None))); Equal(5, results.Distinct().Count()); });
        await Test("foreign completion path rejected", () => { string other = Path.Combine(Root, "foreign.mp4"); WriteMp4(other); return Throws<InvalidDataException>(() => CompletedFile.Validate(other, Path.Combine(Root, "work"), "mp4")); });
        await Test("container mismatch rejected", () => { string path = Path.Combine(Root, "wrong.mp4"); File.WriteAllText(path, "not media at all, just plain text"); return Throws<InvalidDataException>(() => CompletedFile.Validate(path, Root, "mp4")); });
        await Test("engine missing ffprobe reports failure", async () => { string tools = Path.Combine(Root, "missing-tools"); Directory.CreateDirectory(tools); File.WriteAllText(Path.Combine(tools, "yt-dlp.exe"), ""); await ThrowsAsync<InvalidOperationException>(() => new DownloadService(tools, new FakeRunner(), new DiagnosticLog(Path.Combine(Root, "logs"))).DownloadAsync(Request("missing.mp4"), null, CancellationToken.None)); });
        await Test("percent in save directory is literal", async () => {
            var request = Request(); request.OutputPath = Path.Combine(Root, "100% videos", "literal.mp4");
            string path = await Service(new FakeRunner()).DownloadAsync(request, null, CancellationToken.None);
            Check(path.Contains("100% videos") && File.Exists(path));
        });
        await Test("schemeless legacy URL", () => { Equal(YouTubeUrl.Normalize("https://youtu.be/" + Id), YouTubeUrl.Normalize("youtu.be/" + Id)); return Task.CompletedTask; });
        await Test("Deno configuration reused by all operations", () => {
            string directory = Path.Combine(Root, "runtime"); Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "deno.exe"), "");
            var args = YtDlpClient.CommonArguments(directory); Check(args.Contains("--js-runtimes")); Check(args.Contains("deno:" + Path.Combine(directory, "deno.exe"))); Check(!args.Contains("--cookies-from-browser")); return Task.CompletedTask;
        });
        foreach (string version in new[] { "deno 1.0.0", "deno 2.2.9", "invalid", "deno 2.3.0", "deno 2.5.0" })
            await Test("Deno replacement decision " + version, async () => {
                string directory = Path.Combine(Root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "deno.exe"), "");
                Equal(version != "deno 2.3.0" && version != "deno 2.5.0", await ToolDiagnostics.NeedsDenoInstallAsync(directory, new FakeRunner { DenoVersion = version }, CancellationToken.None));
            });
        await Test("failed Deno version check is not cached", async () => {
            string directory = Path.Combine(Root, "runtime-retry"); Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "deno.exe"), "");
            var fake = new FakeRunner { DenoVersion = "deno 1.0.0" };
            await ThrowsAsync<InvalidOperationException>(() => ToolDiagnostics.CheckDenoAsync(directory, fake, CancellationToken.None));
            // A changed executable must trigger a fresh probe.
            File.SetLastWriteTimeUtc(Path.Combine(directory, "deno.exe"), DateTime.UtcNow.AddSeconds(1)); fake.DenoVersion = "deno 2.5.0";
            await ToolDiagnostics.CheckDenoAsync(directory, fake, CancellationToken.None);
        });
        await Test("recover format without exceeding resolution", async () => {
            var fake = new FakeRunner { Failures = 1, ErrorText = "Requested format is not available" };
            var request = Request("fallback.mp4"); request.Height = 720; request.Fps = 30;
            await Service(fake).DownloadAsync(request, null, CancellationToken.None);
            Equal(2, fake.DownloadCalls); Check(string.Join(" ", fake.Commands.Last()).Contains("height<=720")); Check(!string.Join(" ", fake.Commands.Last()).Contains("fps<="));
        });
        await Test("403 refresh limited to one recovery", async () => { var fake = new FakeRunner { Failures = 9, ErrorText = "HTTP 403 Forbidden" }; await ThrowsAsync<InvalidOperationException>(() => Service(fake).DownloadAsync(Request("403.mp4"), null, CancellationToken.None)); Equal(2, fake.DownloadCalls); });
        await Test("provider failure falls back without plugin", async () => { var fake = new FakeRunner { Failures = 1, ErrorText = "PO Token provider unavailable" }; await Service(fake).DownloadAsync(Request("provider.mp4"), null, CancellationToken.None); Check(fake.Commands.Last().Contains("--no-plugin-dirs")); Equal(2, fake.DownloadCalls); });
        await Test("engine cancelled before file creation", async () => { var fake = new FakeRunner(); using (var cts = new CancellationTokenSource()) { cts.Cancel(); await ThrowsAsync<OperationCanceledException>(() => Service(fake).DownloadAsync(Request("never.mp4"), null, cts.Token)); Equal(0, fake.DownloadCalls); } });
    }

    private static DownloadService Service(FakeRunner runner) {
        string tools = Path.Combine(Root, "tools"); Directory.CreateDirectory(tools);
        foreach (string tool in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe" }) { string path = Path.Combine(tools, tool); if (!File.Exists(path)) File.WriteAllText(path, ""); }
        return new DownloadService(tools, runner, new DiagnosticLog(Path.Combine(Root, "logs")));
    }
    private static DownloadRequest Request(string name = "output.mp4") => new DownloadRequest { Url = "https://youtu.be/" + Id, OutputPath = Path.Combine(Root, name) };
    private static void WriteMp4(string path) => File.WriteAllBytes(path, new byte[] { 0, 0, 0, 24, 102, 116, 121, 112, 105, 115, 111, 109, 0, 0, 0, 0 });
    private static async Task Test(string name, Func<Task> test) {
        try { await test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + " " + ex); }
    }
    private static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static Task Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return Task.CompletedTask; } throw new Exception("Expected " + typeof(T).Name); }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception { try { await action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private sealed class Capture : IProgress<DownloadProgress> { private readonly Action<DownloadProgress> action; public Capture(Action<DownloadProgress> action) { this.action = action; } public void Report(DownloadProgress value) => action(value); }

    private sealed class FakeRunner : IProcessRunner {
        public int DownloadCalls, Failures;
        public bool OmitOutput;
        public string OutputExtension = "mp4", ProbeOutput;
        public bool ProbeVideo = true;
        public string ErrorText = "connection reset by peer";
        public string DenoVersion = "deno 2.5.0";
        public readonly List<string[]> Commands = new List<string[]>();
        public Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken token, Action<string> output = null, Action<string> error = null) {
            token.ThrowIfCancellationRequested();
            string[] args = Split(arguments);
            if (Path.GetFileName(executable) == "ffprobe.exe" && args.Contains("-show_streams")) return Task.FromResult(new ProcessResult(0, ProbeOutput ?? ProbeJson("aac", "mov,mp4,m4a,3gp,3g2,mj2", ProbeVideo), ""));
            if (!args.Contains("-o")) return Task.FromResult(new ProcessResult(0, Path.GetFileName(executable) == "deno.exe" ? DenoVersion : "2026.07.04", ""));
            DownloadCalls++;
            Commands.Add(args);
            string path = args[Array.IndexOf(args, "-o") + 1].Replace("%(ext)s", OutputExtension).Replace("%%", "%");
            if (!OmitOutput) { Directory.CreateDirectory(Path.GetDirectoryName(path)); WriteMp4(path); output?.Invoke("GMTFV_FILE:" + JsonConvert.SerializeObject(path)); }
            output?.Invoke("GMTFV_PROGRESS:downloading|50.0%|1MiB/s|10");
            return Task.FromResult(new ProcessResult(DownloadCalls <= Failures ? 1 : 0, "", DownloadCalls <= Failures ? ErrorText : ""));
        }
    }
    private static string[] Split(string arguments) {
        IntPtr pointer = CommandLineToArgvW("fake " + arguments, out int count);
        try { return Enumerable.Range(1, count - 1).Select(i => Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer, i * IntPtr.Size))).ToArray(); }
        finally { LocalFree(pointer); }
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CommandLineToArgvW(string command, out int count);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
    private static int FakeProcess(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        switch (args[0]) {
            case "echo": Console.Write(JsonConvert.SerializeObject(args.Skip(1))); return 0;
            case "fail": Console.Error.WriteLine("requested failure"); return 7;
            case "sleep": Thread.Sleep(30000); return 0;
            case "flood": for (int i = 0; i < 18000; i++) { Console.WriteLine(new string('o', 1024)); if (i % 10 == 0) Console.Error.WriteLine(new string('e', 1024)); } Console.WriteLine("OUT-END"); return 0;
            case "longline": Console.Write(new string('x', 18000000)); Console.WriteLine(); Console.WriteLine("OUT-END"); return 0;
            case "streams": for (int i = 0; i < 1000; i++) { Console.WriteLine(new string('o', 1024)); Console.Error.WriteLine(new string('e', 1024)); } Console.WriteLine("OUT-END"); Console.Error.WriteLine("ERR-END"); return 0;
            case "child": using (var child = Process.Start(new ProcessStartInfo(Self, "--fake sleep") { UseShellExecute = false, CreateNoWindow = true })) { Console.WriteLine(child.Id); Console.Out.Flush(); Thread.Sleep(30000); } return 0;
            default: return 2;
        }
    }
}
