using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;

internal static partial class Program
{
    private static async Task CheckDiagnosticsAsync()
    {
        var log = new DiagnosticLog(Path.Combine(Root, "diagnostics-logs"));
        string Folder(string name, params string[] names) {
            string folder = Path.Combine(Root, "diagnostics-" + name);
            Directory.CreateDirectory(folder);
            foreach (string tool in names) File.WriteAllText(Path.Combine(folder, tool + ".exe"), "fixture");
            return folder;
        }
        string[] all = ["yt-dlp", "ffmpeg", "ffprobe", "deno"];
        var runner = new DiagnosticsRunner();
        var tools = new ToolWorkspaceViewModel(Root, log, CancellationToken.None, runner);
        Check(!tools.HasChecked && !tools.IsReady && tools.Components.All(t => t.Ready == null), "diagnostics never claims readiness before checking");
        string healthy = Folder("ready", all);
        await tools.CheckAsync(healthy);
        Check(tools.IsReady && !tools.Missing && tools.Components.All(t => t.Ready == true), "diagnostics shows all four working features as ready");
        Check(runner.Calls.Any(c => c.Name == "ffprobe.exe" && c.Arguments == "-version"), "shared ffprobe diagnostic uses the supported version argument");
        Check(tools.Report.Contains("deno 2.5.0") && !tools.Guidance.Contains("SHA-256"), "technical versions stay in details while guidance describes the next action");

        using var vm = Create(new FakeBackend(), "diagnostics-render");
        vm.Panel = "Diagnostics";
        var window = new MainWindow(vm) { ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show(); await LayoutAsync(window);
        var view = Find<DiagnosticsView>(window).Single();
        view.DataContext = new { Tools = tools, vm.Settings, vm.PanelCommand, vm.InstallToolsCommand, vm.CancelToolSetupCommand, vm.CheckToolsCommand, vm.ChooseToolsCommand, vm.OpenLogsCommand };
        await LayoutAsync(window);
        var expanders = Find<Expander>(view).ToArray();
        Check(expanders.Length == 2 && expanders.All(e => !e.IsExpanded), "diagnostics starts with troubleshooting and technical information collapsed");
        Check(Find<Button>(view).Single(b => Equals(b.Content, "영상 링크 추가하러 가기")).IsVisible &&
              !Find<Button>(view).Single(b => Equals(b.Content, tools.InstallLabel)).IsVisible, "ready diagnostics leads back to downloading instead of unnecessary installation");
        string previews = Path.GetFullPath(Path.Combine("artifacts", "wpf-preview"));
        Directory.CreateDirectory(previews);
        await Capture(window, Path.Combine(previews, "diagnostics-ready.png"));

        string partial = Folder("missing", "ffmpeg", "deno");
        await tools.CheckAsync(partial); await LayoutAsync(window);
        Check(tools.Missing && !tools.IsReady && tools.Components.Count(t => t.State == "설치 필요") == 2, "missing tools identify the affected user features");
        Check(Find<Button>(view).Single(b => Equals(b.Content, "한 번에 준비하기")).IsVisible, "missing diagnostics exposes one automatic preparation action");
        await Capture(window, Path.Combine(previews, "diagnostics-missing.png"));
        window.Width = window.MinWidth; window.Height = window.MinHeight; await LayoutAsync(window);
        Check(Find<ScrollViewer>(view).First().ScrollableWidth == 0, "diagnostics fits the minimum window without horizontal scrolling");
        await Capture(window, Path.Combine(previews, "diagnostics-compact.png"));
        expanders[1].IsExpanded = true; await LayoutAsync(window);
        var scroll = Find<ScrollViewer>(view).First(); scroll.ScrollToEnd(); await LayoutAsync(window);
        Check(Find<TextBox>(view).All(t => t.IsReadOnly), "diagnostic paths and reports remain selectable read-only information");
        await Capture(window, Path.Combine(previews, "diagnostics-details.png"));
        vm.Settings.ToolFolder = healthy;
        Check(!vm.Tools.HasChecked && vm.Tools.Guidance.Contains("위치"), "editing the settings path invalidates diagnostic readiness");
        await vm.StopAsync();
        bool folderChangeBlocked = false;
        vm.Tools.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(ToolWorkspaceViewModel.IsPreparing) && vm.Tools.IsPreparing)
                folderChangeBlocked = !vm.ChooseToolsCommand.CanExecute(null) && !vm.ChooseOutputCommand.CanExecute(null);
        };
        await vm.Tools.PrepareAsync();
        Check(folderChangeBlocked, "tool preparation disables both folder selectors");
        window.Close(); await vm.StopAsync();

        runner = new DiagnosticsRunner(); runner.Versions["ffmpeg.exe"] = "";
        tools = new ToolWorkspaceViewModel(Root, log, CancellationToken.None, runner);
        string empty = Folder("empty", all);
        await tools.CheckAsync(empty);
        Check(tools.Missing && tools.Components.Single(t => t.Name == "FFmpeg").State == "확인 필요", "empty tool output cannot be reported as healthy");
        runner.Versions["ffmpeg.exe"] = "ffmpeg version 8.0";
        await tools.CheckAsync(empty);
        Check(tools.IsReady, "empty version responses are not cached and recover on retry");
        runner = new DiagnosticsRunner(); runner.Versions["deno.exe"] = "deno 2.2.9";
        tools = new ToolWorkspaceViewModel(Root, log, CancellationToken.None, runner);
        await tools.CheckAsync(Folder("old-deno", all));
        Check(tools.Missing && tools.Components.Single(t => t.Name == "Deno").State == "업데이트 필요", "incompatible Deno has actionable feature-level update guidance");

        tools = new ToolWorkspaceViewModel("relative-tools", log, CancellationToken.None, runner);
        Check(await tools.PrepareAsync() == null && !tools.IsBusy && tools.Status.Contains("마치지 못") && tools.Guidance.Contains("다시 시도"), "preparation failures expose friendly recovery guidance and release busy state");
        Check(tools.Report.Contains("Unknown") && tools.Report.Contains("ArgumentException"), "preparation failure details preserve the actual diagnostic cause");

        runner = new DiagnosticsRunner { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        tools = new ToolWorkspaceViewModel(Root, log, CancellationToken.None, runner);
        Task checking = tools.CheckAsync(Folder("stale", all));
        Check(tools.IsBusy && !tools.IsReady && await tools.PrepareAsync() == null, "tool checks cannot overlap preparation");
        tools.Invalidate(); runner.Gate.SetResult(); await checking;
        Check(!tools.HasChecked && !tools.IsReady && tools.Components.All(t => t.Ready == null), "a folder change discards in-flight results for the previous folder");
        await tools.CheckAsync("");
        Check(tools.Missing && !tools.IsBusy && tools.Status.Contains("확인하지 못"), "invalid tool paths leave a retryable diagnostic state");
        using var cancellation = new CancellationTokenSource();
        runner = new DiagnosticsRunner { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        tools = new ToolWorkspaceViewModel(Root, log, cancellation.Token, runner);
        checking = tools.CheckAsync(Folder("cancel", all)); cancellation.Cancel();
        try { await checking; throw new Exception("diagnostic cancellation ignored"); }
        catch (OperationCanceledException) { Check(!tools.IsBusy, "diagnostic cancellation releases busy state"); }
    }

    private sealed class DiagnosticsRunner : IProcessRunner
    {
        public Dictionary<string, string> Versions { get; } = new() { ["yt-dlp.exe"] = "2026.10.22", ["ffmpeg.exe"] = "ffmpeg version 8.0", ["ffprobe.exe"] = "ffprobe version 8.0", ["deno.exe"] = "deno 2.5.0" };
        public ConcurrentBag<(string Name, string Arguments)> Calls { get; } = [];
        public TaskCompletionSource? Gate;
        public async Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken token, Action<string>? output = null, Action<string>? error = null) {
            string name = Path.GetFileName(executable);
            Calls.Add((name, arguments));
            if (Gate != null) await Gate.Task.WaitAsync(token);
            token.ThrowIfCancellationRequested();
            return new ProcessResult(0, Versions[name], "");
        }
    }
}
