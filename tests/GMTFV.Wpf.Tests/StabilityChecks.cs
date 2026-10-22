using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using GMTFV.Core;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Models;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;

internal static partial class Program {
    private static async Task CheckStabilityAsync() {
        var cases = new (Exception Error, ToolSetupError Kind)[] {
            (new HttpRequestException(HttpRequestError.NameResolutionError, "dns"), ToolSetupError.Dns),
            (new HttpRequestException("wrapped", new SocketException((int)SocketError.HostNotFound)), ToolSetupError.Dns),
            (new HttpRequestException(HttpRequestError.SecureConnectionError, "tls"), ToolSetupError.Tls),
            (new HttpRequestException("tls", new AuthenticationException()), ToolSetupError.Tls),
            (new HttpRequestException(HttpRequestError.ProxyTunnelError, "proxy"), ToolSetupError.Proxy),
            (new HttpRequestException("403", null, HttpStatusCode.Forbidden), ToolSetupError.Forbidden),
            (new HttpRequestException("429", null, HttpStatusCode.TooManyRequests), ToolSetupError.RateLimited),
            (new HttpRequestException("503", null, HttpStatusCode.ServiceUnavailable), ToolSetupError.Server),
            (new Win32Exception(5), ToolSetupError.Permission), (new Win32Exception(32), ToolSetupError.Locked),
            (new Win32Exception(225), ToolSetupError.Antivirus), (new Win32Exception(112), ToolSetupError.DiskFull),
            (new PathTooLongException(), ToolSetupError.Path), (new InvalidDataException(), ToolSetupError.Archive),
            (new CryptographicException(), ToolSetupError.Checksum), (new ToolExecutionException(7, "bad executable"), ToolSetupError.Execution),
            (new InvalidOperationException("Deno 2.3.0 이상이 필요합니다."), ToolSetupError.Execution),
            (new IOException("wrapper", new Win32Exception(33)), ToolSetupError.Locked)
        };
        foreach (var test in cases) Check(ToolSetupDiagnostics.Classify(test.Error).Kind == test.Kind, "installation diagnostic uses actual code/chain: " + test.Kind);
        Check(ToolSetupDiagnostics.Classify(new ToolRateLimitException(HttpStatusCode.Forbidden, "API limit")).Kind == ToolSetupError.RateLimited, "GitHub rate-limit diagnosis preserves the actual HTTP 403 status");
        Check(ToolSetupDiagnostics.Classify(new UnauthorizedAccessException()).Kind != ToolSetupError.Antivirus, "access denial never implies antivirus detection");
        var handler = new TransferHandler((attempt, _) => Task.FromResult(Response(attempt < 3 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK)));
        using (var client = new HttpClient(handler)) using (var output = new MemoryStream()) {
            var events = new List<ToolSetupProgress>();
            await new ToolTransfer(client).DownloadAsync("https://example.test/tool", output, 1024, new SetupCapture(events.Add), "FFmpeg", CancellationToken.None);
            Check(handler.Calls == 3 && output.Length == 128 && events.Any(p => p.Phase.Contains("재시도")) && events.Last().Percent == 100, "429 Retry-After retries are bounded and real byte progress is reported");
        }
        foreach (var code in new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.TooManyRequests }) {
            handler = new TransferHandler((_, _) => { var response = Response(code); response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(120)); return Task.FromResult(response); });
            using var client = new HttpClient(handler); using var output = new MemoryStream();
            try { await new ToolTransfer(client).DownloadAsync("https://example.test/tool", output, 1024, null, "tool", CancellationToken.None); throw new Exception("HTTP accepted"); }
            catch (HttpRequestException) { Check(handler.Calls == 1, "permanent/long Retry-After response does not hammer server: " + code); }
        }
        handler = new TransferHandler((_, _) => Task.FromResult(Response(HttpStatusCode.ServiceUnavailable)));
        using (var client = new HttpClient(handler)) using (var output = new MemoryStream()) {
            try { await new ToolTransfer(client).DownloadAsync("https://example.test/tool", output, 1024, null, "tool", CancellationToken.None); throw new Exception("503 accepted"); }
            catch (HttpRequestException) { Check(handler.Calls == 3, "5xx retries stop at three"); }
        }
        handler = new TransferHandler(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return Response(HttpStatusCode.OK); });
        using (var client = new HttpClient(handler)) using (var output = new MemoryStream()) {
            try { await new ToolTransfer(client, headerTimeout: TimeSpan.FromMilliseconds(10), totalTimeout: TimeSpan.FromMilliseconds(300)).DownloadAsync("https://example.test/tool", output, 1024, null, "tool", CancellationToken.None); throw new Exception("timeout ignored"); }
            catch (ToolTransferTimeoutException ex) { Check(ex.Kind == ToolSetupError.DownloadTimeout, "whole-transfer deadline includes headers and backoff"); }
        }
        handler = new TransferHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StalledStream()) }));
        using (var client = new HttpClient(handler)) using (var output = new MemoryStream()) using (var cancellation = new CancellationTokenSource()) {
            try { await new ToolTransfer(client, idleTimeout: TimeSpan.FromMilliseconds(10)).DownloadAsync("https://example.test/tool", output, 1024,
                new SetupCapture(p => { if (p.Phase.Contains("장시간")) cancellation.Cancel(); }), "tool", cancellation.Token); throw new Exception("stall ignored"); }
            catch (OperationCanceledException) { Check(handler.Calls == 1, "idle timeout gives explicit guidance and cancellation interrupts retry wait"); }
        }
        handler = new TransferHandler((_, _) => { var response = Response(HttpStatusCode.OK); response.Content.Headers.ContentLength = 256; return Task.FromResult(response); });
        using (var client = new HttpClient(handler)) using (var output = new MemoryStream()) {
            try { await new ToolTransfer(client).DownloadAsync("https://example.test/tool", output, 1024, null, "tool", CancellationToken.None); throw new Exception("truncation ignored"); }
            catch (HttpRequestException) { Check(handler.Calls == 3 && output.Length == 128, "truncated downloads retry from zero without appending stale data"); }
        }
        handler = new TransferHandler((_, _) => Task.FromResult(Response(HttpStatusCode.OK)));
        using (var client = new HttpClient(handler)) using (var output = new MemoryStream()) using (var cancellation = new CancellationTokenSource()) {
            try { await new ToolTransfer(client).DownloadAsync("https://example.test/tool", output, 1024, new SetupCapture(_ => cancellation.Cancel()), "tool", cancellation.Token); throw new Exception("cancel ignored"); }
            catch (OperationCanceledException) { Check(handler.Calls == 1, "user cancellation does not retry a transfer"); }
        }
        string cacheRoot = Path.Combine(Root, "한글 사용자 도구 경로 with spaces");
        using (var releases = new CountingReleaseFixture()) using (var client = new HttpClient(releases)) {
            releases.FailDeno = true;
            try { await new ToolSetupService(cacheRoot, client, new EngineRunner()).InstallAsync(null, CancellationToken.None); throw new Exception("Deno failure ignored"); }
            catch (HttpRequestException) { Check(releases.YtDownloads == 1 && Directory.EnumerateFiles(Path.Combine(cacheRoot, "verified-cache")).Count() == 1, "failed installation preserves the verified tool cache"); }
            releases.FailDeno = false;
            var events = new List<ToolSetupProgress>();
            string installed = await new ToolSetupService(cacheRoot, client, new EngineRunner()).InstallAsync(null, CancellationToken.None, new SetupCapture(events.Add));
            Check(releases.YtDownloads == 1 && File.Exists(Path.Combine(installed, "ffmpeg.exe")) && events.Any(p => p.Phase.Contains("캐시")), "retry reuses SHA-verified yt-dlp with Korean and spaced install path");
            Check(events.Where(p => p.Step is 4 or 5 or 6 or 7 or 8).All(p => p.Percent == null), "hash/extraction/execution stages do not reuse download percentages");
            string cachedYt = Directory.EnumerateFiles(Path.Combine(cacheRoot, "verified-cache"), "*-yt-dlp.exe").Single();
            File.WriteAllText(cachedYt, "damaged private cache");
            await new ToolSetupService(cacheRoot, client, new EngineRunner()).InstallAsync(null, CancellationToken.None);
            Check(releases.YtDownloads == 2 && Directory.Exists(installed), "corrupt cached tool is downloaded again without replacing a working bundle");
            using var cancellation = new CancellationTokenSource();
            try { await new ToolSetupService(cacheRoot, client, new EngineRunner()).InstallAsync(null, cancellation.Token,
                new SetupCapture(p => { if (p.Step == 5) cancellation.Cancel(); })); throw new Exception("extraction cancellation ignored"); }
            catch (OperationCanceledException) { Check(Directory.Exists(installed) && !Directory.EnumerateDirectories(cacheRoot, ".prepare-*").Any(), "cancellation during extraction removes only staging and preserves activated tools"); }
        }
        using var vm = Create(new FakeBackend(), "stability-options");
        vm.Settings.IsVideo = false; vm.SelectedFormat = "original";
        Check(!vm.CanSetBitrate && vm.FormatDescription.Contains("원본") && vm.FileNamePreview.EndsWith(".[원본 확장자]"), "original audio disables bitrate and previews the actual-extension placeholder");
        var saved = JsonSerializer.Deserialize<PreviewSettings>(JsonSerializer.Serialize(vm.Settings))!; saved.Validate();
        Check(!saved.IsVideo && saved.Format == "original", "original audio settings JSON roundtrips");
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk");
        using (var editor = new ItemSettingsViewModel(vm.Items[0], vm.Settings, new FakeBackend(), CancellationToken.None)) {
            editor.UseDefaults = false; editor.Format = "original"; editor.Apply();
        }
        string list = Path.Combine(Root, "original-audio.json"); await vm.SaveListAsync(list);
        using (var loaded = Create(new FakeBackend(), "original-restored")) {
            await loaded.LoadListAsync(list);
            Check(loaded.Items.Single().Options is { IsVideo: false, Format: "original" }, "original audio per-item option survives queue JSON");
        }
        var item = vm.Items[0]; item.State = DownloadState.Downloading; item.Percent = 75;
        item.ApplyProgress(new DownloadProgress { State = DownloadState.Merging, Stage = DownloadStage.Merge, Message = "병합 중" });
        await Task.Delay(1050); item.TickElapsed();
        Check(item.Percent == null && item.IsIndeterminate && item.ActivityText.Contains("1초 경과"), "unmeasured merge clears stream percent and shows elapsed time");
        item.ApplyProgress(new DownloadProgress { State = DownloadState.Converting, Stage = DownloadStage.EncodeAudio, Message = "오디오 인코딩", Percent = 50 });
        Check(item.ProgressText == "50%" && !item.IsIndeterminate, "direct FFmpeg measured progress is visible for audio encoding");
        item.State = DownloadState.Failed;
        Check(item.Percent == null && item.ProgressText == "", "failure cannot keep a previous success percentage");
        await Task.Delay(20);
        int summaryChanges = 0; vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MainWindowViewModel.Summary)) summaryChanges++; };
        for (int percent = 0; percent < 100; percent++) item.Percent = percent;
        await Task.Delay(20);
        Check(summaryChanges == 0, "100 row progress events do not invalidate the item-count queue summary");
        var window = new MainWindow(vm) { ShowActivated = false, ShowInTaskbar = false, Left = -30000, Top = -30000, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); await LayoutAsync(window);
        var quickRadios = Find<RadioButton>(window).Where(r => r.GroupName == "QuickMode").ToArray();
        Check(quickRadios.Length == 2 && quickRadios.Count(r => r.IsChecked == true) == 1, "main screen exposes exactly one selected media radio");
        quickRadios.Single(r => Equals(r.Content, "영상 다운로드")).IsChecked = true; await LayoutAsync(window);
        Check(vm.Settings.IsVideo && vm.Settings.Format == "mp4" && quickRadios.Count(r => r.IsChecked == true) == 1, "radio switch retains one selection and resets incompatible extension");
        await vm.StopAsync(); window.Close();
        var backend = new FakeBackend { Slow = true };
        using var closing = Create(backend, "shutdown-download"); await closing.AddUrlsAsync("https://youtu.be/abcdefghijk");
        Task running = closing.StartAsync(); await Task.Delay(20); await closing.StopAsync();
        Check(running.IsCompleted && closing.Items.Single().State == DownloadState.Cancelled, "shutdown awaits directly started downloads and releases resources");
        using var importing = Create(new FakeBackend { SlowMetadata = true }, "cancel-list-operation");
        Task import = importing.LoadListAsync(list);
        for (int wait = 0; importing.Items.Count == 0 && wait < 100; wait++) await Task.Delay(10);
        Check(importing.IsListBusy && importing.WorkspaceStatus.Contains("목록") && !importing.LoadListCommand.CanExecute(null), "list parsing/metadata operation exposes state and prevents overlapping imports");
        importing.CancelListCommand.Execute(null); await import;
        Check(!importing.IsListBusy && importing.Items.All(i => i.State == DownloadState.Cancelled), "list cancellation reaches metadata slots and restores normal UI state");
    }
    private static HttpResponseMessage Response(HttpStatusCode code) {
        var response = new HttpResponseMessage(code) { Content = new ByteArrayContent(new byte[128]) };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero); return response;
    }
    private sealed class SetupCapture(Action<ToolSetupProgress> action) : IProgress<ToolSetupProgress> { public void Report(ToolSetupProgress value) => action(value); }
    private sealed class TransferHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => response(++Calls, token);
    }
    private sealed class StalledStream : MemoryStream {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) { await Task.Delay(Timeout.Infinite, token); return 0; }
    }
    private sealed class CountingReleaseFixture : HttpMessageHandler {
        private readonly HttpMessageInvoker inner = new(new ToolReleaseFixture());
        public bool FailDeno;
        public int YtDownloads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) {
            if (request.RequestUri!.AbsolutePath.EndsWith("yt-dlp.exe")) YtDownloads++;
            if (FailDeno && request.RequestUri.AbsolutePath.EndsWith("deno-x86_64-pc-windows-msvc.zip")) return Task.FromResult(Response(HttpStatusCode.Forbidden));
            return inner.SendAsync(request, token);
        }
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
}
