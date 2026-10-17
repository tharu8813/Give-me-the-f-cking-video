using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GMTFV.Core;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;

internal static partial class Program
{
    private static async Task CaptureMotionDemoAsync()
    {
        using var vm = Create(new FakeBackend(), "motion-demo");
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        vm.Items[0].Title = "다운로드 여정 · 모션 시연"; vm.Items[1].Title = "목록 재배치와 문제 해결 · 테스트";
        var window = new MainWindow(vm) { Width = 1000, Height = 720, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show(); await LayoutAsync(window); Motion.ReduceMotion = false;
        string folder = Path.GetFullPath(Path.Combine("artifacts", "rework-motion-frames")); Directory.CreateDirectory(folder);
        using var cancellation = new CancellationTokenSource();
        var item = vm.Items[0];
        for (int frame = 0; frame < 160; frame++) {
            if (frame == 6) vm.Items.Insert(1, new DownloadItemViewModel("https://youtu.be/12345678901") { Title = "새 영상의 정보를 확인하는 중", State = DownloadState.Metadata });
            if (frame == 18) { vm.Items[1].State = DownloadState.Ready; vm.Items[1].Title = "새 영상이 준비되었습니다"; }
            if (frame == 25) { item.Cancellation = cancellation; item.State = DownloadState.Downloading; }
            if (frame is >= 25 and < 60) { item.Percent = (frame - 25) * 100 / 35; item.Message = "시연용 진행 데이터"; }
            if (frame == 60) { item.State = DownloadState.Merging; item.Percent = null; item.Message = "영상과 오디오를 병합하는 중"; }
            if (frame == 70) { item.State = DownloadState.Validating; item.Message = "완료 파일을 확인하는 중"; }
            if (frame == 80) { item.Cancellation = null; item.State = DownloadState.Completed; item.Percent = 100; item.OutputPath = Path.Combine(Root, "시연.mp4"); item.Message = "시연 완료"; }
            if (frame == 88) vm.Panel = "Settings";
            if (frame == 114) vm.Panel = "";
            if (frame == 126) { vm.Items[2].ErrorKind = DownloadError.Network; vm.Items[2].Error = "시연용 네트워크 오류"; vm.Items[2].State = DownloadState.Failed; vm.Failure = vm.Items[2]; }
            if (frame == 150) vm.Failure = null;
            await Task.Delay(30); window.UpdateLayout(); CaptureFrame(window, Path.Combine(folder, $"{frame:D3}.png"));
        }
        await vm.StopAsync(); window.Close(); await Task.Delay(100);
        Check(true, "captured real WPF motion frames with scripted test states");
    }
    private static async Task CheckShowcaseAsync()
    {
        var backend = new FakeBackend { Slow = true };
        using var vm = Create(backend, "showcase");
        vm.Settings.OutputFolder = Path.Combine(Root, "showcase-output");
        var window = new MainWindow(vm) { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show(); await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        vm.Items[0].Title = "미디어 작업대 · 다운로드 상태 테스트";
        vm.Items[1].Title = "영상별 오디오 설정 · 복구 흐름 테스트";
        vm.Items[1].Options = new() { IsVideo = false, Format = "mp3" };
        string artifacts = Path.Combine("artifacts", "wpf-preview");
        Task download = vm.StartAsync(); await Task.Delay(80);
        await Capture(window, Path.Combine(artifacts, "downloading.png"));
        Check(!vm.CanEditSettings && vm.Items.All(i => i.State == DownloadState.Downloading), "download workspace locks defaults while displaying active work");
        vm.Panel = "Settings"; await LayoutAsync(window);
        Check(Find<CheckBox>(Find<SettingsView>(window).Single()).Single(c => Equals(c.Content, "모션 줄이기")).IsEnabled, "motion preference remains accessible during downloads");
        vm.Panel = "";
        vm.CancelAll(); await download;
        backend.Slow = false; backend.FailDownload = true;
        await vm.RetryAsync(vm.Items[0]); vm.Failure = vm.Items[0];
        await Capture(window, Path.Combine(artifacts, "failure-guidance.png"));
        Check(vm.Failure.ErrorTitle.Contains("연결") && !vm.Failure.ErrorAction.Contains("fixture"), "failure guidance separates user action from diagnostic detail");
        vm.Failure = null; backend.FailDownload = false;
        await vm.RetryAsync(vm.Items[0]); await vm.RetryAsync(vm.Items[1]);
        await Capture(window, Path.Combine(artifacts, "completed.png"));
        Check(vm.Items.All(i => i.HasOutput) && vm.OverallProgress == 100, "completed workspace exposes both file actions");

        Motion.ReduceMotion = false;
        var samples = new List<double>(); var clock = Stopwatch.StartNew(); double last = 0;
        var timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) => { double now = clock.Elapsed.TotalMilliseconds; samples.Add(now - last); last = now; };
        timer.Start();
        for (int i = 0; i < 24; i++) { vm.Panel = i % 2 == 0 ? "Settings" : ""; await Task.Delay(18); }
        timer.Stop(); Motion.ReduceMotion = true; vm.Panel = "";
        Console.WriteLine($"METRIC offscreen dispatcher sample: n={samples.Count}, mean={samples.DefaultIfEmpty().Average():F1}ms, max={samples.DefaultIfEmpty().Max():F1}ms (not display FPS)");
        Check(samples.Count > 0 && vm.IsWorkspaceEnabled, "dispatcher continues processing rapid motion reversals");
        await vm.StopAsync(); window.Close(); await Task.Delay(80);
    }
    private static async Task CheckLiveMetadataAsync()
    {
        var log = new GMTFV.DownloadEngine.DiagnosticLog(Path.Combine(Root, "Logs"));
        using var vm = new MainWindowViewModel(new DownloadBackend(log), new SettingsStore(Root), log);
        vm.Settings.ToolFolder = File.ReadAllText(Path.Combine("artifacts", "rework-live-tool-bundle.txt")).Trim();
        var window = new MainWindow(vm) { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show();
        await vm.AddUrlsAsync("https://www.youtube.com/watch?v=jNQXAC9IVRw");
        Check(vm.Items.Single() is { State: DownloadState.Ready, Title: "Me at the zoo", Metadata: not null }, "real WPF backend loads public YouTube metadata with the new tool bundle");
        await Task.Delay(1600);
        await Capture(window, Path.Combine("artifacts", "wpf-preview", "live-metadata.png"));
        await vm.StopAsync(); window.Close(); await Task.Delay(80);
    }
    private static async Task CheckLiveToolSetupAsync()
    {
        string folder = await Task.Run(() => new ToolSetupService(Path.Combine(Root, "live-tools")).InstallAsync(new Progress<string>(Console.WriteLine), CancellationToken.None));
        Check(new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }.All(n => File.Exists(Path.Combine(folder, n))), "live official tool bundle passes checksums and executable checks");
        File.WriteAllText(Path.Combine("artifacts", "rework-live-tool-bundle.txt"), folder);
    }
    private static async Task CheckToolSetupAsync()
    {
        string root = Path.Combine(Root, "private-tools"); Directory.CreateDirectory(root);
        string sentinel = Path.Combine(root, "existing-tool.exe"); File.WriteAllText(sentinel, "keep");
        using var http = new HttpClient(new ToolReleaseFixture());
        var setup = new ToolSetupService(root, http, new EngineRunner());
        string installed = await setup.InstallAsync(null, CancellationToken.None);
        Check(new[] { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" }.All(n => File.Exists(Path.Combine(installed, n))) && File.Exists(Path.Combine(installed, "notices", "ffmpeg-LICENSE")), "verified tool bundle contains executables and distribution notices");
        string updated = await setup.InstallAsync(null, CancellationToken.None);
        Check(updated != installed && Directory.Exists(installed) && File.ReadAllText(sentinel) == "keep", "tool updates use fresh bundles and preserve existing tools");
        foreach (string fault in new[] { "checksum", "missing-digest", "foreign-url", "archive" }) {
            using var invalid = new HttpClient(new ToolReleaseFixture(fault));
            try { await new ToolSetupService(root, invalid, new EngineRunner()).InstallAsync(null, CancellationToken.None); throw new Exception("invalid tools accepted: " + fault); }
            catch (InvalidDataException) { Check(Directory.Exists(installed) && !Directory.EnumerateDirectories(root, ".prepare-*").Any() && File.ReadAllText(sentinel) == "keep", "tool failure preserves installed bundle and clears staging: " + fault); }
        }
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        try { await setup.InstallAsync(null, cancellation.Token); throw new Exception("tool cancellation ignored"); }
        catch (OperationCanceledException) { Check(Directory.Exists(installed) && !Directory.EnumerateDirectories(root, ".prepare-*").Any(), "cancelled tool preparation preserves existing tools"); }
    }
    private static async Task CheckReworkAsync()
    {
        using var vm = Create(new FakeBackend(), "rework");
        vm.SelectedMode = "오디오";
        Check(!vm.Settings.IsVideo && vm.Settings.Format == "mp3" && vm.SettingsStatus.StartsWith("변경됨"), "quick audio mode updates valid defaults and unsaved status");
        vm.SelectedMode = "영상"; vm.SelectedHeight = 720;
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        Check(vm.Items.All(i => i.Selection.Contains("720p")), "quick quality reaches inherited item selections");
        vm.Settings.FileNameTemplate = "%title%_%num3%";
        Check(vm.FileNamePreview == "나의 영상_001.mp4", "filename template has a live safe preview");
        vm.SaveSettingsCommand.Execute(null); await Task.Delay(10);
        Check(vm.SettingsStatus == "설정 저장됨", "saving defaults clears unsaved state");
        vm.Items[0].State = DownloadState.Downloading; vm.Items[0].Percent = 100;
        Check(vm.OverallProgress == 0, "overall completion does not claim unvalidated downloads as complete");
        vm.Items[0].State = DownloadState.Completed; vm.Items[0].OutputPath = Path.Combine(Root, "finished.mp4");
        Check(vm.OverallProgress == 50 && vm.Items[0].HasOutput, "completed files expose file actions and item-based total");
        vm.Items[1].State = DownloadState.Merging; vm.Items[1].Percent = null;
        Check(vm.Items[1].IsIndeterminate && vm.Items[1].ProgressText == "" && vm.Items[1].Phase == 3, "postprocessing uses real phase without fabricated percentage");
        vm.Items[1].State = DownloadState.Cancelled;
        Check(vm.Items[1].Phase == -1 && vm.Items[1].CanRetry, "cancelled journey stops and permits manual retry");
        vm.RemoveSelectedCommand.Execute(new[] { vm.Items[0], vm.Items[1] }); await Task.Delay(220);
        Check(vm.IsEmpty, "multi-selection removal removes queue entries together");
        vm.ResetSettingsCommand.Execute(null);
        Check(vm.SelectedHeight == 1080 && vm.Settings.FileNameTemplate == "%title%_%id%", "reset restores defaults without deleting user files");

        var window = new MainWindow(vm) { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show(); await LayoutAsync(window);
        var list = Find<ListBox>(window).Single();
        Motion.ReduceMotion = false;
        vm.Panel = "Settings"; await Task.Delay(35); vm.Panel = ""; await Task.Delay(20); vm.Panel = "Diagnostics";
        Motion.ReduceMotion = true;
        Check(window.FindName("PanelOverlay") is FrameworkElement { Visibility: Visibility.Visible, Opacity: 1, IsHitTestVisible: true }, "reducing motion immediately settles interrupted panel transitions");
        vm.Panel = ""; await LayoutAsync(window);
        Motion.ReduceMotion = false;
        using var cancel = new CancellationTokenSource();
        vm.Items.Add(new DownloadItemViewModel("https://youtu.be/abcdefghijk") { State = DownloadState.Metadata, Cancellation = cancel });
        await LayoutAsync(window);
        var skeleton = Find<DownloadItemView>(window).SelectMany(Find<Border>).First(b => Motion.GetPulse(b));
        await Task.Delay(30); Motion.ReduceMotion = true;
        Check(!skeleton.HasAnimatedProperties, "reduced motion immediately stops visible skeleton clocks");
        vm.Items[0].State = DownloadState.Ready; vm.Items[0].Cancellation = null;
        Motion.ReduceMotion = false;
        vm.Items.Insert(0, new DownloadItemViewModel("https://youtu.be/lmnopqrstuv") { State = DownloadState.Ready });
        await Task.Delay(50);
        Motion.ReduceMotion = true;
        Check(Find<ListBoxItem>(list).All(r => r.RenderTransform == Transform.Identity), "reduced motion clears queue reflow transforms");
        list.SelectedItems.Add(vm.Items[0]); list.SelectedItems.Add(vm.Items[1]); await LayoutAsync(window);
        var queue = Find<QueueView>(window).Single();
        Check(queue.FindName("SelectionBar") is FrameworkElement { Visibility: Visibility.Visible }, "multiple selection reveals explicit bulk actions");
        vm.RemoveSelectedCommand.Execute(list.SelectedItems); await Task.Delay(60);
        Check(vm.IsEmpty, "bulk actions snapshot a live selection before removing containers");
        var toggle = new CheckBox { Content = "fixture", Width = 100, Height = 40 };
        ((Grid)window.Content).Children.Add(toggle); toggle.ApplyTemplate(); Motion.ReduceMotion = false;
        toggle.IsChecked = true; await Task.Delay(25); Motion.ReduceMotion = true;
        var knob = (Border)toggle.Template.FindName("Knob", toggle);
        Check(knob.RenderTransform is TransformGroup group && group.Children[1] is TranslateTransform { X: 14, HasAnimatedProperties: false }, "reduced motion settles toggle at its selected position");
        ((Grid)window.Content).Children.Remove(toggle);

        string broken = Path.Combine(Root, "blocked-settings"); Directory.CreateDirectory(Path.Combine(broken, "settings.json"));
        File.WriteAllText(Path.Combine(broken, "settings.json", "keep"), "keep");
        try { new SettingsStore(broken).Save(new()); throw new Exception("blocked settings save unexpectedly succeeded"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Check(File.ReadAllText(Path.Combine(broken, "settings.json", "keep")) == "keep" && !Directory.EnumerateFiles(broken, "*.tmp").Any(), "failed settings replacement preserves data and clears its unique staging file"); }
        await vm.StopAsync(); window.Close(); await Task.Delay(60);
        string lastToast = vm.Toast; vm.Notify("late notification");
        Check(vm.Toast == lastToast, "notifications arriving after window disposal are ignored");
    }
    private sealed class ToolReleaseFixture(string fault = "") : HttpMessageHandler
    {
        private static byte[] Zip(params (string Name, string Text)[] files) {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true)) foreach (var file in files) { using var writer = new StreamWriter(archive.CreateEntry(file.Name).Open()); writer.Write(file.Text); }
            return stream.ToArray();
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            byte[] yt = Encoding.UTF8.GetBytes("yt-dlp fixture"), deno = Zip(("deno.exe", "fixture")), ffmpeg = fault == "archive" ? Zip(("../ffmpeg.exe", "fixture")) : Zip(("package/bin/ffmpeg.exe", "fixture"), ("package/bin/ffprobe.exe", "fixture"), ("package/LICENSE", "license fixture"));
            static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
            string path = request.RequestUri!.AbsolutePath;
            byte[] data;
            if (path.Contains("/releases/latest")) {
                bool isDeno = path.Contains("denoland"); string repo = isDeno ? "denoland/deno" : "yt-dlp/yt-dlp", name = isDeno ? "deno-x86_64-pc-windows-msvc.zip" : "yt-dlp.exe";
                string digest = fault == "missing-digest" ? "" : "sha256:" + (fault == "checksum" ? new string('0', 64) : Hash(isDeno ? deno : yt));
                data = JsonSerializer.SerializeToUtf8Bytes(new { assets = new[] { new { name, digest, browser_download_url = fault == "foreign-url" ? "https://example.com/tool.exe" : "https://github.com/" + repo + "/releases/download/vtest/" + name } } });
            }
            else if (path.EndsWith(".sha256")) data = Encoding.UTF8.GetBytes(Hash(ffmpeg));
            else data = path.EndsWith("yt-dlp.exe") ? yt : path.Contains("deno-x86") ? deno : ffmpeg;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(data) });
        }
    }
}
