using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GMTFV.Core;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Models;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;

internal static partial class Program
{
    private static readonly string Root = Path.GetFullPath(Path.Combine("artifacts", "wpf-tests", Guid.NewGuid().ToString("N")));
    private static int passed;
    private static readonly BindingErrors Errors = new();
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--sleep")) { Thread.Sleep(30000); return 0; }
        if (args.Contains("--echo")) { Console.WriteLine("net10-process-ok"); return 0; }
        Directory.CreateDirectory(Root);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/GMTFV.Wpf;component/Themes/Theme.xaml", UriKind.Relative) });
        Motion.ReduceMotion = true;
        PresentationTraceSources.DataBindingSource.Listeners.Add(Errors);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        var task = args.Contains("--app-update-smoke") ? CheckLiveAppUpdateAsync() : args.Contains("--tool-setup-smoke") ? CheckLiveToolSetupAsync() : args.Contains("--live-metadata-smoke") ? CheckLiveMetadataAsync() : args.Contains("--motion-demo") ? CaptureMotionDemoAsync() : args.Contains("--queue-check") ? CheckQueueOrderAsync() : RunAsync();
        task.ContinueWith(_ => app.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Background), TaskScheduler.Default);
        Dispatcher.Run();
        try { task.GetAwaiter().GetResult(); Console.WriteLine($"PASS {passed} WPF checks"); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(Errors); }
    }
    private static MainWindowViewModel Create(FakeBackend backend, string name) => new(backend, new SettingsStore(Path.Combine(Root, name)), new DiagnosticLog(Path.Combine(Root, "Logs")));
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); passed++; Console.WriteLine("PASS " + message); }
    private static async Task RunAsync()
    {
        await CheckAppUpdatesAsync();
        var processRunner = new ProcessRunner();
        var echo = await processRunner.RunAsync(Environment.ProcessPath!, "--echo", TimeSpan.FromSeconds(10), CancellationToken.None);
        Check(echo.ExitCode == 0 && echo.Output.Contains("net10-process-ok"), ".NET 10 native process runner");
        using (var cts = new CancellationTokenSource(100)) {
            try { await processRunner.RunAsync(Environment.ProcessPath!, "--sleep", TimeSpan.FromSeconds(10), cts.Token); throw new Exception("process cancellation failed"); }
            catch (OperationCanceledException) { Check(true, ".NET 10 process job cancellation"); }
        }
        string tools = Path.Combine(Root, "engine-tools"); Directory.CreateDirectory(tools);
        foreach (string name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }) File.WriteAllText(Path.Combine(tools, name), "fixture");
        var engine = new DownloadService(tools, new EngineRunner(), new DiagnosticLog(Path.Combine(Root, "engine-logs")));
        var engineRequest = new DownloadRequest { Url = "https://youtu.be/abcdefghijk", OutputPath = Path.Combine(Root, "engine-output.mp4") };
        string first = await engine.DownloadAsync(engineRequest, null, CancellationToken.None);
        string second = await engine.DownloadAsync(engineRequest, null, CancellationToken.None);
        Check(first != second && File.Exists(first) && File.Exists(second), ".NET 10 engine validates and publishes without overwrite");
        var store = new SettingsStore(Path.Combine(Root, "settings"));
        var settings = new PreviewSettings { OutputFolder = Path.Combine(Root, "media"), Format = "mkv", Concurrency = 3 };
        store.Save(settings); Check(store.Load().Format == "mkv" && store.Load().Concurrency == 3, "settings round trip");
        settings.Concurrency = 17;
        try { store.Save(settings); throw new Exception("invalid settings accepted"); } catch (ArgumentException) { Check(store.Load().Concurrency == 3, "invalid save preserves previous settings"); }

        var backend = new FakeBackend(); using var vm = Create(backend, "queue");
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://www.youtube.com/watch?v=abcdefghijk https://example.com/test https://youtu.be/lmnopqrstuv");
        Check(vm.Items.Count == 2 && vm.Items.All(i => i.State == DownloadState.Ready), "URL validation, deduplication and metadata");
        vm.Settings.OutputFolder = Path.Combine(Root, "output"); vm.Settings.IsVideo = false; vm.Settings.Format = "mp3"; vm.Settings.AudioBitrate = 256;
        await vm.StartAsync();
        Check(vm.Items.All(i => i.State == DownloadState.Completed && i.Percent == 100), "downloads reach completion");
        Check(backend.Requests.All(r => !r.IsVideo && r.Extension == "mp3" && r.AudioBitrate == 256 && r.OutputPath.StartsWith(vm.Settings.OutputFolder)), "folder and audio options reach engine adapter");
        Check(backend.Peak == 2, "concurrent downloads use shared queue slots");
        string listPath = Path.Combine(Root, "queue.json");
        File.WriteAllText(listPath, "old list");
        await vm.SaveListAsync(listPath);
        using (var listData = JsonDocument.Parse(File.ReadAllText(listPath)))
            Check(listData.RootElement.EnumerateArray().Select(e => e.GetProperty("Url").GetString()).SequenceEqual(vm.Items.Select(i => i.Url)), "JSON list safely replaces an existing file");
        await vm.SaveListAsync(Path.Combine(Root, "queue.txt"));
        Check(File.ReadAllLines(Path.Combine(Root, "queue.txt")).SequenceEqual(vm.Items.Select(i => i.Url)), "text list saves normalized URLs");
        vm.UrlInput = "draft URL";
        await vm.AddUrlsAsync(vm.Items[0].Url);
        Check(vm.UrlInput == "draft URL", "external URL additions preserve the input draft");
        vm.UrlInput = vm.Items[0].Url; vm.AddCommand.Execute(null); await Task.Delay(30);
        Check(vm.UrlInput == "", "submitting the input clears only the submitted draft");
        using (var loaded = Create(new FakeBackend(), "loaded-list")) {
            loaded.UrlInput = "draft URL";
            await loaded.LoadListAsync(listPath);
            Check(loaded.Items.Select(i => i.Url).SequenceEqual(vm.Items.Select(i => i.Url)) && loaded.UrlInput == "draft URL", "JSON list imports URLs without erasing the input draft");
            string oversized = Path.Combine(Root, "oversized.txt"); File.WriteAllBytes(oversized, new byte[2 * 1024 * 1024 + 1]);
            try { await loaded.LoadListAsync(oversized); throw new Exception("oversized list accepted"); }
            catch (InvalidDataException) { Check(loaded.Items.Count == 2, "oversized list is rejected without changing the queue"); }
        }
        using (var cancelledSave = Create(new FakeBackend(), "cancelled-save")) {
            await cancelledSave.StopAsync();
            string original = File.ReadAllText(listPath);
            try { await cancelledSave.SaveListAsync(listPath); throw new Exception("cancelled list save succeeded"); }
            catch (OperationCanceledException) { Check(File.ReadAllText(listPath) == original && !Directory.EnumerateFiles(Root, "*.tmp").Any(), "cancelled list save preserves the original and removes temporary files"); }
        }
        string blockedPath = Path.Combine(Root, "existing-directory"); Directory.CreateDirectory(blockedPath);
        File.WriteAllText(Path.Combine(blockedPath, "keep.txt"), "keep");
        try { await vm.SaveListAsync(blockedPath); throw new Exception("invalid replacement succeeded"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Check(File.ReadAllText(Path.Combine(blockedPath, "keep.txt")) == "keep" && !Directory.EnumerateFiles(Root, "*.tmp").Any(), "failed list replacement preserves existing data and removes temporary files");
        }

        backend = new FakeBackend { FailMetadata = true }; using var retry = Create(backend, "retry");
        await retry.AddUrlsAsync("https://youtu.be/abcdefghijk");
        Check(retry.Items[0].State == DownloadState.Failed && retry.Items[0].HasError, "metadata failure is visible");
        backend.FailMetadata = false; await retry.RetryAsync(retry.Items[0]);
        Check(retry.Items[0].State == DownloadState.Ready, "metadata retry returns to review step");
        backend.FailDownload = true; retry.Settings.OutputFolder = Path.Combine(Root, "retry-output");
        await retry.StartAsync(); Check(retry.Items[0].State == DownloadState.Failed && retry.Items[0].CanRetry, "download failure enables retry");
        backend.FailDownload = false; await retry.RetryAsync(retry.Items[0]);
        Check(retry.Items[0].State == DownloadState.Completed, "download retry completes");

        backend = new FakeBackend(); using var batchRetry = Create(backend, "batch-retry");
        await batchRetry.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        batchRetry.Settings.OutputFolder = Path.Combine(Root, "batch-retry-output");
        batchRetry.Items[0].State = DownloadState.Failed; batchRetry.Items[0].Error = "test failure";
        await batchRetry.RetryFailedAsync();
        Check(backend.Requests.Count == 1 && backend.Requests.Single().Url == batchRetry.Items[0].Url && batchRetry.Items[1].State == DownloadState.Ready, "retry failed leaves unrelated ready items untouched");
        backend = new FakeBackend { FailMetadata = true }; using var metadataBatch = Create(backend, "metadata-batch");
        await metadataBatch.AddUrlsAsync("https://youtu.be/abcdefghijk");
        backend.FailMetadata = false;
        await metadataBatch.AddUrlsAsync("https://youtu.be/lmnopqrstuv");
        metadataBatch.Settings.OutputFolder = Path.Combine(Root, "metadata-batch-output");
        await metadataBatch.RetryFailedAsync();
        Check(backend.Requests.Count == 1 && metadataBatch.Items[0].State == DownloadState.Completed && metadataBatch.Items[1].State == DownloadState.Ready, "batch retry reloads failed metadata without starting other items");

        backend = new FakeBackend { Slow = true }; using var cancel = Create(backend, "cancel");
        await cancel.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        cancel.Settings.Concurrency = 1; cancel.Settings.OutputFolder = Path.Combine(Root, "cancel-output");
        Task running = cancel.StartAsync(); await Task.Delay(70);
        cancel.CancelAll(); await running;
        Check(cancel.Items.All(i => i.State == DownloadState.Cancelled) && backend.Peak == 1, "cancel active and queued jobs without slot leaks");

        backend = new FakeBackend { Slow = true }; using var individual = Create(backend, "individual");
        await individual.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        individual.Settings.OutputFolder = Path.Combine(Root, "individual-output");
        Task pair = individual.StartAsync(); await Task.Delay(40);
        individual.CancelCommand.Execute(individual.Items[0]); await Task.Delay(30);
        Check(individual.Items[0].State == DownloadState.Cancelled && individual.Items[1].State == DownloadState.Downloading, "individual cancellation leaves other downloads running");
        individual.CancelAll(); await pair;

        backend = new FakeBackend { SlowMetadata = true }; using var shutdown = Create(backend, "shutdown");
        Task adding = shutdown.AddUrlsAsync("https://youtu.be/abcdefghijk"); await Task.Delay(20); await shutdown.StopAsync(); await adding;
        Check(shutdown.Items[0].State == DownloadState.Cancelled, "shutdown awaits metadata cancellation");

        await CheckQueueOrderAsync();
        await CheckReworkAsync();
        await CheckToolSetupAsync();
        await CheckShowcaseAsync();
        await CheckItemFeaturesAsync();
        using var render = Create(new FakeBackend(), "render");
        var window = new MainWindow(render) { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show(); await Task.Delay(80);
        Check(window.Width <= SystemParameters.WorkArea.Width && window.Height <= SystemParameters.WorkArea.Height, "initial window fits the desktop work area");
        string artifacts = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "artifacts", "wpf-preview")); Directory.CreateDirectory(artifacts);
        await Capture(window, Path.Combine(artifacts, "empty.png")); Check(render.IsEmpty, "empty state rendered");
        double initialWidth = window.Width, initialHeight = window.Height;
        window.Width = window.MinWidth; window.Height = window.MinHeight; await LayoutAsync(window);
        await Capture(window, Path.Combine(artifacts, "empty-compact.png"));
        window.Width = initialWidth; window.Height = initialHeight; await LayoutAsync(window);
        using var renderCancellation = new CancellationTokenSource();
        int summaryChanges = 0;
        render.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MainWindowViewModel.Summary)) summaryChanges++; };
        for (int i = 0; i < 1000; i++) render.Items.Add(new DownloadItemViewModel("https://youtu.be/abcdefghijk") { Title = i % 2 == 0 ? "Night drive — a collection of moments" : "작업할 때 듣는 플레이리스트 · 서울의 밤", Metadata = new VideoMetadata { Id = "abcdefghijk", Title = "테스트 영상", UploadDate = new DateTime(2024, 1, 2), Formats = [new VideoFormat { Height = 1080, Fps = 60 }] }, Channel = "GMTFV Studio", Duration = "12:48", Selection = "MP4 · 1080p", Cancellation = i == 0 ? renderCancellation : null, State = i == 1 ? DownloadState.Failed : i == 0 ? DownloadState.Downloading : DownloadState.Ready, Percent = i == 0 ? 64 : null, Message = i == 1 ? "네트워크 연결을 확인한 후 다시 시도하세요." : i == 0 ? "다운로드 중 64%" : "준비 완료", Error = i == 1 ? "네트워크 연결을 확인하세요.\n상세 로그에서 오류 내용을 확인할 수 있습니다." : "" });
        Check(summaryChanges == 0, "bulk queue changes defer summary notifications");
        await LayoutAsync(window); await Capture(window, Path.Combine(artifacts, "queue.png"));
        Check(summaryChanges == 1 && render.Summary.StartsWith("1000개"), "bulk queue changes publish one accurate summary");
        var list = Find<ListBox>(window).Single(); int realized = Find<ListBoxItem>(list).Count();
        Check(realized > 0 && realized < 40, $"1000 rows virtualized ({realized} realized)");
        Check(render.Items.Select(i => i.QueueNumber).SequenceEqual(Enumerable.Range(1, 1000)), "bulk additions assign consecutive queue numbers");
        list.ScrollIntoView(render.Items[^1]); await LayoutAsync(window);
        var lastRow = Find<ListBoxItem>(list).Single(r => ReferenceEquals(r.DataContext, render.Items[^1]));
        Check(Find<TextBlock>(lastRow).Any(t => t.Text == "1000") && Find<ListBoxItem>(list).Count() < 40, "recycled rows display their actual queue number at the end of a large list");
        list.ScrollIntoView(render.Items[0]); await LayoutAsync(window);
        foreach (string panel in new[] { "Settings", "Diagnostics", "About" }) {
            render.Panel = panel; await LayoutAsync(window);
            Check(Find<Border>(list).First().Background is SolidColorBrush surface && list.Background is SolidColorBrush expected && surface.Color == expected.Color, $"queue keeps its dark background with {panel} open");
            if (panel == "Settings") {
                var tabs = Find<TabControl>(window).Single();
                Check(tabs.Items.Count == 5, "settings have five categories");
                for (int tab = 0; tab < tabs.Items.Count; tab++) {
                    tabs.SelectedIndex = tab; await LayoutAsync(window);
                    if (tab == 0) {
                        var reduce = Find<CheckBox>(Find<SettingsView>(window).Single()).Single(c => Equals(c.Content, "모션 줄이기"));
                        reduce.IsChecked = true;
                        Check(Motion.ReduceMotion && render.Settings.ReduceMotion, "motion preference applies immediately without saving");
                        reduce.IsChecked = false;
                    }
                    await Capture(window, Path.Combine(artifacts, $"settings-tab-{tab}.png"));
                }
                tabs.SelectedIndex = 2; await LayoutAsync(window);
                var format = Find<ComboBox>(window).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "저장 형식");
                format.IsDropDownOpen = true; await LayoutAsync(window); format.SelectedItem = "mkv"; format.IsDropDownOpen = false;
                Check(render.Settings.Format == "mkv", "format dropdown binding and popup");
                var mode = Find<CheckBox>(Find<SettingsView>(window).Single()).Single(c => c.Content is string text && text.StartsWith("영상 다운로드"));
                mode.IsChecked = false; render.ModeChangedCommand.Execute(null); await LayoutAsync(window);
                Check(!render.Settings.IsVideo && render.Settings.Format == "mp3" && (string?)format.SelectedItem == "mp3", "audio mode preserves a valid format selection");
                Check((string?)format.SelectionBoxItem == "mp3" && Find<TextBlock>(format).Any(t => t.Text == "mp3"), "audio format is visibly rendered after mode change");
                mode.IsChecked = true; render.ModeChangedCommand.Execute(null); await LayoutAsync(window);
                Check(render.Settings.IsVideo && render.Settings.Format == "mp4" && (string?)format.SelectedItem == "mp4", "video mode restores a valid format selection");
                Check((string?)format.SelectionBoxItem == "mp4" && Find<TextBlock>(format).Any(t => t.Text == "mp4"), "video format is visibly rendered after mode change");
            }
            await Capture(window, Path.Combine(artifacts, panel.ToLowerInvariant() + ".png"));
        }
        render.Panel = ""; render.Failure = render.Items[1]; await LayoutAsync(window); await Capture(window, Path.Combine(artifacts, "failure.png"));
        Check(Find<Border>(list).First().Background is SolidColorBrush failureSurface && list.Background is SolidColorBrush failureExpected && failureSurface.Color == failureExpected.Color, "queue keeps its dark background with the failure dialog open");
        render.Failure = null; window.Width = window.MinWidth; window.Height = window.MinHeight; await LayoutAsync(window); await Capture(window, Path.Combine(artifacts, "compact.png"));
        Check(list.ActualHeight >= 2 * Find<ListBoxItem>(list).First().ActualHeight, "minimum window shows at least two complete queue rows");
        Motion.ReduceMotion = false;
        render.Panel = "Settings"; await Task.Delay(280); Check(window.FindName("PanelOverlay") is FrameworkElement { Visibility: Visibility.Visible }, "animated panel opens");
        render.Panel = ""; await Task.Delay(200); Check(window.FindName("PanelOverlay") is FrameworkElement { Visibility: Visibility.Collapsed }, "animated panel closes");
        render.RemoveCommand.Execute(render.Items.Last()); await Task.Delay(250); Check(render.Items.Count == 999, "animated row removal");
        render.Failure = render.Items[1]; await Task.Delay(280);
        render.Failure = null; await Task.Delay(200);
        render.Panel = "Settings"; await Task.Delay(280);
        render.Settings.ReduceMotion = true; render.SaveSettingsCommand.Execute(null);
        render.Panel = ""; render.Failure = render.Items[1]; await LayoutAsync(window);
        Check(window.FindName("FailureOverlay") is FrameworkElement { Visibility: Visibility.Visible, Opacity: 1 }, "failure dialog remains visible after enabling reduced motion");
        await Capture(window, Path.Combine(artifacts, "failure-reduced-motion.png"));
        render.Failure = null; await LayoutAsync(window);
        Check(render.IsWorkspaceEnabled && window.FindName("FailureOverlay") is FrameworkElement { Visibility: Visibility.Collapsed }, "reduced-motion dialog closes and restores workspace input");
        await CheckItemDialogAsync(window, render, artifacts);
        await CheckScrollingAsync(window, list);
        await CheckInteractionsAsync(window, render);
        Check(Errors.Messages.Count == 0, "no WPF binding errors: " + string.Join("\n", Errors.Messages));
        var closeErrors = new List<Exception>();
        DispatcherUnhandledExceptionEventHandler captureCloseError = (_, e) => { closeErrors.Add(e.Exception); e.Handled = true; };
        Application.Current.DispatcherUnhandledException += captureCloseError;
        try { window.Close(); window.Close(); await Task.Delay(150); }
        finally { Application.Current.DispatcherUnhandledException -= captureCloseError; }
        Check(closeErrors.Count == 0 && !window.IsLoaded, "repeated close requests finish cleanup once without dispatcher exceptions");
        Console.WriteLine("Rendered previews: " + artifacts);
    }
    private static async Task CheckItemFeaturesAsync()
    {
        var parsed = DownloadBackend.ParseSubtitles("{\"subtitles\":{\"en\":[{}],\"ko\":[{}],\"bad.*\":[{}],\"fr\":[]},\"automatic_captions\":{\"en\":[{}]}}");
        Check(parsed.Count == 3 && parsed[0].Language == "ko" && parsed.Count(t => t.Automatic) == 1, "subtitle parser includes manual and automatic tracks and rejects invalid languages");
        var backend = new FakeBackend(); using var vm = Create(backend, "item-options");
        vm.Settings.OutputFolder = Path.Combine(Root, "item-media");
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        using (var editor = new ItemSettingsViewModel(vm.Items[0], vm.Settings, backend, CancellationToken.None)) {
            Check(editor.UseDefaults && editor.Qualities.Any(q => q.Height == 1080 && q.Fps == 60), "new items inherit defaults and expose available quality/FPS pairs");
            editor.UseDefaults = false; editor.IsVideo = false; editor.Format = "mp3"; editor.Apply();
        }
        vm.Settings.Format = "mkv"; vm.Settings.Height = 720; vm.RefreshSelections();
        Check(!vm.Items[0].Options!.IsVideo && vm.Items[0].Options!.Format == "mp3" && vm.Items[1].Options == null, "per-video audio options survive changes to common defaults");
        var best = new DownloadItemViewModel(vm.Items[1].Url) { Metadata = vm.Items[1].Metadata, Options = new ItemDownloadOptions() };
        using (var editor = new ItemSettingsViewModel(best, vm.Settings, backend, CancellationToken.None))
            Check(editor.SelectedQuality.Height == null, "explicit best quality does not inherit the common height when reopened");
        string path = Path.Combine(Root, "item-options.json"); await vm.SaveListAsync(path);
        using (var importing = Create(new FakeBackend { SlowMetadata = true }, "item-importing")) {
            Task loading = importing.LoadListAsync(path);
            for (int wait = 0; importing.Items.Count == 0 && wait < 100; wait++) await Task.Delay(10);
            Check(importing.Items.FirstOrDefault()?.Options is { IsVideo: false, Format: "mp3" }, "imported overrides are attached before metadata can make a row downloadable");
            await importing.StopAsync(); await loading;
        }
        using (var restored = Create(new FakeBackend(), "item-restored")) {
            await restored.LoadListAsync(path);
            Check(restored.Items[0].Options is { IsVideo: false, Format: "mp3" } && restored.Items[1].Options == null, "JSON queue restores per-item overrides and inherited defaults");
            string legacy = Path.Combine(Root, "legacy-options.json");
            File.WriteAllText(legacy, "[{\"Url\":\"https://youtu.be/ZYXWVUTSRQP\",\"IsTypeVideo\":true,\"SubType\":\"webm\",\"Quality\":\"1080p\",\"Fps\":60}]");
            await restored.LoadListAsync(legacy);
            Check(restored.Items.Last().Options is { Format: "webm", Height: 1080, Fps: 60 }, "legacy WinForms queue restores format and quality/FPS");
            File.WriteAllText(legacy, "[{\"Url\":\"https://youtu.be/12345678901\",\"Options\":{\"IsVideo\":false,\"Format\":\"mp4\"}}]");
            try { await restored.LoadListAsync(legacy); throw new Exception("invalid item options accepted"); }
            catch (ArgumentException) { Check(restored.Items.Count == 3, "invalid imported item options cannot change the queue"); }
        }
        await vm.StartAsync();
        Check(backend.Requests.Single(r => r.Url == vm.Items[0].Url) is { IsVideo: false, Extension: "mp3" } && backend.Requests.Single(r => r.Url == vm.Items[1].Url) is { Extension: "mkv", Height: 720 }, "item overrides and current defaults reach the engine independently");
        using (var editor = new ItemSettingsViewModel(vm.Items[0], vm.Settings, backend, CancellationToken.None)) {
            editor.UseDefaults = false; editor.IsVideo = true; editor.Format = "webm"; editor.SelectedQuality = editor.Qualities.Single(q => q.Height == 1080 && q.Fps == 60); editor.Apply();
            vm.Items[0].State = DownloadState.Ready; await vm.StartAsync();
            Check(backend.Requests.Any(r => r.Url == vm.Items[0].Url && r.Extension == "webm" && r.Height == 1080 && r.Fps == 60), "selected video quality and FPS reach the engine");
            editor.UseDefaults = true; editor.Apply(); Check(vm.Items[0].Options == null, "switching back to defaults removes the per-item override");
            await editor.LoadSubtitlesAsync(); editor.SelectedSubtitle = editor.Subtitles.Single(t => t.Automatic); await editor.DownloadSubtitleAsync();
            Check(backend.SubtitleRequests.Single().Automatic && Directory.EnumerateFiles(vm.Settings.OutputFolder, "*.srt").Any(), "selected automatic subtitles are saved as SRT");
        }
        backend.SlowSubtitles = true;
        using (var editor = new ItemSettingsViewModel(vm.Items[0], vm.Settings, backend, CancellationToken.None)) {
            Task loading = editor.LoadSubtitlesAsync(); await Task.Delay(20); await editor.StopAsync(); await loading;
            Check(!editor.IsBusy && editor.Status.Contains("취소"), "closing item settings awaits subtitle cancellation");
        }
        backend.SlowSubtitles = false; backend.FailSubtitles = true;
        using (var editor = new ItemSettingsViewModel(vm.Items[0], vm.Settings, backend, CancellationToken.None)) {
            editor.LoadSubtitlesCommand.Execute(null); await Task.Delay(40);
            Check(!editor.IsBusy && editor.Status.Contains("fixture failure"), "subtitle errors are visible without crashing the UI");
        }
    }
    private static async Task CheckItemDialogAsync(MainWindow window, MainWindowViewModel vm, string artifacts)
    {
        Check(window.Icon != null && !window.Title.Contains("Preview") && !vm.Version.Contains("Preview"), "application uses its icon and has no Preview branding");
        Check(!Find<Button>(window).Any(b => Equals(b.Content, "≡")), "unused hamburger navigation button is removed");
        vm.EditItemCommand.Execute(vm.Items[2]); await LayoutAsync(window);
        Check(vm.ItemSettings != null && !vm.IsWorkspaceEnabled, "per-item settings dialog opens and blocks the workspace");
        var editor = vm.ItemSettings!;
        await editor.LoadSubtitlesAsync(); editor.UseDefaults = false; editor.IsVideo = false; await LayoutAsync(window);
        var format = Find<ComboBox>(window).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "영상별 저장 형식");
        Check((string?)format.SelectionBoxItem == "mp3", "per-item audio format is visibly rendered");
        await Capture(window, Path.Combine(artifacts, "item-settings.png"));
        var scroller = Find<ScrollViewer>(Find<ItemSettingsView>(window).Single()).First();
        scroller.ScrollToBottom(); await LayoutAsync(window);
        await Capture(window, Path.Combine(artifacts, "item-settings-options.png"));
        vm.SaveItemCommand.Execute(null); await Task.Delay(200);
        Check(vm.ItemSettings == null && vm.Items[2].Options is { IsVideo: false, Format: "mp3" } && vm.IsWorkspaceEnabled, "applying item settings restores workspace input");
    }
    private static async Task CheckScrollingAsync(Window window, ListBox list)
    {
        var viewer = Find<ScrollViewer>(list).First(); Motion.ReduceMotion = false;
        viewer.ScrollToTop(); await LayoutAsync(window);
        viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent });
        await Task.Delay(45); double intermediate = viewer.VerticalOffset; await Task.Delay(220);
        Check(viewer.VerticalOffset > 0 && viewer.VerticalOffset >= intermediate && SmoothScroll.GetEnabled(viewer), "queue mouse wheel scrolls smoothly in pixel units");
        Check(Find<ListBoxItem>(list).Count() < 40, "smooth scrolling preserves queue virtualization");
        Motion.ReduceMotion = true; double before = viewer.VerticalOffset;
        viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent }); await LayoutAsync(window);
        Check(viewer.VerticalOffset > before && !viewer.HasAnimatedProperties, $"reduced motion scrolls without animation (before={before}, after={viewer.VerticalOffset}, animated={viewer.HasAnimatedProperties})");
        Motion.ReduceMotion = false; before = viewer.VerticalOffset;
        for (int wheel = 0; wheel < 3; wheel++) {
            viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent }); await Task.Delay(25);
        }
        await Task.Delay(230);
        Check(viewer.VerticalOffset > before + 96, "repeated wheel events accumulate their target offsets");
        viewer.ScrollToTop(); await LayoutAsync(window);
    }
    private static async Task CheckInteractionsAsync(MainWindow window, MainWindowViewModel vm)
    {
        Motion.ReduceMotion = false;
        var button = new Button { Content = "test", Width = 100, Height = 40 };
        var root = (Grid)window.Content; root.Children.Add(button); button.ApplyTemplate();
        button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
        ((Border)button.Template.FindName("Body", button)).RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
        await Task.Delay(160); button.IsEnabled = false; await Task.Delay(20);
        Check(button.Opacity < 0.5, "hover feedback does not override disabled button appearance");
        root.Children.Remove(button);

        vm.Panel = "Settings"; await Task.Delay(280);
        var content = (ContentControl)window.FindName("PanelContent"); object settingsView = content.Content;
        vm.Panel = "";
        Check(ReferenceEquals(content.Content, settingsView), "closing panel retains content throughout the exit animation");
        await Task.Delay(200);
        vm.Panel = "Settings"; vm.Failure = vm.Items[1]; await Task.Delay(280);
        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Check(vm.Failure == null && vm.Panel == "Settings", "Escape closes only the top dialog and preserves the settings panel");
        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        await Task.Delay(200);
        Check(vm.IsWorkspaceEnabled, "second Escape closes the panel and restores workspace input");
        for (int i = 0; i < 12; i++) { vm.Panel = new[] { "Settings", "Diagnostics", "About", "" }[i % 4]; await Task.Delay(15); }
        await Task.Delay(250);
        Check(vm.IsWorkspaceEnabled && window.FindName("PanelOverlay") is FrameworkElement { Visibility: Visibility.Collapsed }, "rapid navigation settles without an invisible input-blocking overlay");
        var fileData = new DataObject(); fileData.SetData(DataFormats.FileDrop, new[] { Path.Combine(Root, "queue.json") });
        vm.UrlInput = "draft URL";
        window.RaiseEvent(DragArgs(window, fileData, DragDrop.PreviewDropEvent)); await Task.Delay(100);
        Check(vm.Items.Any(i => i.Url.EndsWith("lmnopqrstuv")) && vm.UrlInput == "draft URL", "dropping a queue file imports it and preserves the input draft");
        vm.Panel = "Settings"; int count = vm.Items.Count;
        var blockedData = new DataObject(DataFormats.UnicodeText, "https://youtu.be/ZYXWVUTSRQP");
        var drag = DragArgs(window, blockedData, DragDrop.PreviewDragOverEvent); window.RaiseEvent(drag);
        window.RaiseEvent(DragArgs(window, blockedData, DragDrop.PreviewDropEvent)); await Task.Delay(50);
        Check(drag.Effects == DragDropEffects.None && vm.Items.Count == count, "open panels block drops into the disabled workspace");
        vm.Panel = ""; await Task.Delay(200);
        var urlBox = (TextBox)window.FindName("UrlBox");
        vm.UrlInput = string.Join("\n", Enumerable.Repeat("https://youtu.be/abcdefghijk", 8)); await LayoutAsync(window);
        Check(urlBox.ActualHeight <= 72 && Find<ScrollViewer>(urlBox).Single().ScrollableHeight > 0, "bulk URL input is height-limited and scrollable");
        vm.UrlInput = "";
    }
    private static DragEventArgs DragArgs(Window window, IDataObject data, RoutedEvent routedEvent)
    {
        var constructor = typeof(DragEventArgs).GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
        var args = (DragEventArgs)constructor.Invoke([data, DragDropKeyStates.None, DragDropEffects.Copy, window, new Point()]);
        args.RoutedEvent = routedEvent;
        return args;
    }
    private static async Task LayoutAsync(Window window) { await Task.Delay(40); window.UpdateLayout(); await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); }
    private static async Task Capture(Window window, string path)
    {
        if (Motion.Enabled) await Task.Delay(Motion.Spatial + 40);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        CaptureFrame(window, path);
    }
    private static void CaptureFrame(Window window, string path)
    {
        var content = (FrameworkElement)window.Content;
        var bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(path); encoder.Save(stream);
    }
    private static IEnumerable<T> Find<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T item) yield return item; foreach (T nested in Find<T>(child)) yield return nested; }
    }
    private sealed class BindingErrors : TraceListener
    {
        public List<string> Messages { get; } = [];
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) Messages.Add(message); }
        public override void WriteLine(string? message) => Write(message);
    }
    private sealed class FakeBackend : IDownloadBackend
    {
        public bool FailMetadata, FailDownload, Slow, SlowMetadata, SlowSubtitles, FailSubtitles;
        public List<SubtitleTrack> SubtitleRequests { get; } = [];
        private int active;
        public int Peak;
        public ConcurrentBag<DownloadRequest> Requests { get; } = [];
        public async Task<IReadOnlyList<SubtitleTrack>> GetSubtitlesAsync(string url, string toolFolder, CancellationToken token) { await Task.Delay(SlowSubtitles ? 10000 : 5, token); if (FailSubtitles) throw new IOException("subtitle fixture failure"); return [new("ko", false, "한국어"), new("en", true, "영어")]; }
        public Task<string> DownloadSubtitleAsync(string url, SubtitleTrack track, string target, string toolFolder, CancellationToken token) { token.ThrowIfCancellationRequested(); SubtitleRequests.Add(track); File.WriteAllText(target, "1\n00:00:00,000 --> 00:00:01,000\nsubtitle"); return Task.FromResult(target); }
        public async Task<VideoMetadata> GetMetadataAsync(string url, string toolFolder, CancellationToken token)
        {
            await Task.Delay(SlowMetadata ? 10000 : 10, token);
            if (FailMetadata) throw new FileNotFoundException("yt-dlp.exe missing");
            return new VideoMetadata { Id = url.Split('=').Last(), Title = "테스트 영상", Uploader = "테스트 채널", Duration = TimeSpan.FromSeconds(90), UploadDate = new DateTime(2024, 1, 2), Formats = [new VideoFormat { Height = 1080, Fps = 60 }, new VideoFormat { Height = 720, Fps = 30 }] };
        }
        public async Task<string> DownloadAsync(DownloadRequest request, string toolFolder, IProgress<DownloadProgress> progress, CancellationToken token)
        {
            Requests.Add(request); active++; Peak = Math.Max(Peak, active);
            try { progress.Report(new DownloadProgress { State = DownloadState.Downloading, Percent = 42, Message = "테스트 다운로드" }); await Task.Delay(Slow ? 10000 : 40, token); if (FailDownload) throw new IOException("network is unreachable"); return request.OutputPath; }
            finally { active--; }
        }
    }
    private sealed class EngineRunner : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken token, Action<string>? output = null, Action<string>? error = null)
        {
            token.ThrowIfCancellationRequested();
            string marker = CommandLine.Quote("-o") + " ";
            int index = arguments.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) return Task.FromResult(new ProcessResult(0, Path.GetFileName(executable) == "deno.exe" ? "deno 2.5.0" : "test version", ""));
            string quoted = arguments[(index + marker.Length)..];
            string path = quoted[1..quoted.IndexOf('"', 1)].Replace("%(ext)s", "mp4").Replace("%%", "%");
            File.WriteAllBytes(path, [0, 0, 0, 24, 102, 116, 121, 112, 105, 115, 111, 109, 0, 0, 0, 0]);
            output?.Invoke("GMTFV_FILE:" + System.Text.Json.JsonSerializer.Serialize(path));
            return Task.FromResult(new ProcessResult(0, "", ""));
        }
    }
}
