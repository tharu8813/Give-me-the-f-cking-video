using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Models;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;

internal static partial class Program
{
    private static async Task CheckLiveAppUpdateAsync()
    {
        var service = new AppUpdateService();
        var current = typeof(MainWindowViewModel).Assembly.GetName().Version!;
        var release = await service.CheckAsync(new Version(4, 0, 0, 0), CancellationToken.None);
        Check(release?.Version == current, $"official latest release offers the {current} installer to a 4.0.0.0 client");
        string installer = await service.DownloadAsync(release!, Path.Combine("artifacts", "app-update-smoke"), null, CancellationToken.None);
        Check(File.Exists(installer), "actual GitHub installer download passes release size and SHA-256 validation without execution");
        Check(await service.CheckAsync(current, CancellationToken.None) == null, "published current version correctly reports no update");
    }

    private static async Task CheckAppUpdatesAsync()
    {
        string directory = Path.Combine(Root, "app-updates");
        var fixture = new AppReleaseFixture();
        using var http = new HttpClient(fixture);
        var service = new AppUpdateService(http);
        var release = (await service.CheckAsync(new Version(4, 0, 0, 0), CancellationToken.None))!;
        Check(release.Version == new Version(4, 1, 0, 0), "app updater discovers a newer stable four-part version");
        Check(await service.CheckAsync(new Version(4, 1, 0, 0), CancellationToken.None) == null && await service.CheckAsync(new Version(5, 0, 0, 0), CancellationToken.None) == null, "app updater never installs the same version or downgrades");
        string verified = await service.DownloadAsync(release, directory, null, CancellationToken.None);
        Check(File.ReadAllBytes(verified).SequenceEqual(AppReleaseFixture.Bytes), "app installer is published only after size and SHA-256 verification");
        File.WriteAllBytes(verified, Enumerable.Repeat((byte)1, AppReleaseFixture.Bytes.Length).ToArray());
        try { await AppUpdateService.VerifyAsync(release, verified, CancellationToken.None); throw new Exception("tampered installer accepted"); }
        catch (InvalidDataException) { Check(true, "cached installers are reverified before execution"); }
        foreach (string fault in new[] { "digest", "foreign-url", "size", "asset", "duplicate", "tag" }) {
            using var invalid = new HttpClient(new AppReleaseFixture(fault));
            try { await new AppUpdateService(invalid).CheckAsync(new Version(4, 0, 0, 0), CancellationToken.None); throw new Exception("invalid release accepted: " + fault); }
            catch (InvalidDataException) { Check(true, "app updater rejects unsafe release metadata: " + fault); }
        }
        foreach (string kind in new[] { "draft", "prerelease" }) {
            using var preview = new HttpClient(new AppReleaseFixture(kind));
            Check(await new AppUpdateService(preview).CheckAsync(new Version(4, 0, 0, 0), CancellationToken.None) == null, "app updater ignores " + kind + " releases");
        }
        foreach (string fault in new[] { "corrupt", "truncated", "oversized", "network" }) {
            using var invalid = new HttpClient(new AppReleaseFixture(fault));
            string staging = Path.Combine(directory, fault);
            try { await new AppUpdateService(invalid).DownloadAsync(release, staging, null, CancellationToken.None); throw new Exception("invalid download accepted: " + fault); }
            catch (Exception ex) when (ex is InvalidDataException or HttpRequestException) {
                Check(!Directory.EnumerateFileSystemEntries(staging).Any(), "failed installer download clears only its staging directory: " + fault);
            }
        }
        var blocked = new AppReleaseFixture { BlockDownload = true };
        using var blockedHttp = new HttpClient(blocked);
        bool idle = true; int launched = 0;
        var model = new AppUpdateViewModel(new AppUpdateService(blockedHttp), Path.Combine(directory, "deferred"), () => idle, _ => { launched++; return Task.CompletedTask; }, new DiagnosticLog(Path.Combine(Root, "update-logs")), CancellationToken.None);
        await model.CheckAsync();
        idle = false;
        Check(model.HasUpdate && !model.InstallCommand.CanExecute(null), "update installation is unavailable while workspace work is active");
        idle = true; Task preparing = model.InstallAsync();
        await blocked.DownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Check(!model.InstallCommand.CanExecute(null) && !model.CheckCommand.CanExecute(null), "repeated update clicks cannot start overlapping operations");
        idle = false; blocked.AllowDownload.TrySetResult(); await preparing;
        Check(launched == 0 && model.Status.Contains("작업"), "work started during installer download prevents application shutdown");
        idle = true; await model.InstallAsync();
        Check(launched == 1 && blocked.Downloads == 1, "verified installer is reused when the workspace becomes idle");

        var cancelled = new AppReleaseFixture { BlockDownload = true };
        using var cancelledHttp = new HttpClient(cancelled);
        var cancelModel = new AppUpdateViewModel(new AppUpdateService(cancelledHttp), Path.Combine(directory, "cancelled"), () => true, _ => throw new Exception("cancelled installer launched"), new DiagnosticLog(Path.Combine(Root, "update-logs")), CancellationToken.None);
        await cancelModel.CheckAsync(); Task downloading = cancelModel.InstallAsync();
        await cancelled.DownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancelModel.CancelCommand.Execute(null); await cancelModel.StopAsync(); await downloading;
        Check(!cancelModel.IsBusy && cancelModel.Status.Contains("취소") && !Directory.EnumerateFileSystemEntries(Path.Combine(directory, "cancelled")).Any(), "update cancellation and shutdown leave no partial installer");

        using var offlineHttp = new HttpClient(new AppReleaseFixture("offline"));
        var offline = new AppUpdateViewModel(new AppUpdateService(offlineHttp), directory, () => true, _ => Task.CompletedTask, new DiagnosticLog(Path.Combine(Root, "update-logs")), CancellationToken.None);
        await offline.CheckAsync();
        Check(!offline.HasUpdate && !offline.IsBusy && offline.CheckCommand.CanExecute(null) && offline.Status.Contains("네트워크"), "offline update checks provide a retry without blocking application use");

        string settingsPath = Path.Combine(Root, "update-startup");
        var store = new SettingsStore(settingsPath);
        store.Save(new PreviewSettings { CheckForAppUpdates = false, ToolFolder = Path.Combine(Root, "missing-update-tools") });
        var startupFixture = new AppReleaseFixture();
        using var startupHttp = new HttpClient(startupFixture);
        var backend = new FakeBackend { Slow = true };
        using var vm = new MainWindowViewModel(backend, store, new DiagnosticLog(Path.Combine(Root, "update-logs")), new AppUpdateService(startupHttp));
        Check(!vm.Settings.CheckForAppUpdates && new PreviewSettings().CheckForAppUpdates, "automatic update preference defaults on and preserves an explicit opt-out");
        await vm.InitializeAsync();
        Check(startupFixture.Checks == 0, "startup respects the automatic update opt-out");
        vm.Settings.CheckForAppUpdates = true;
        File.WriteAllText(Path.Combine(settingsPath, "update-queue.json"), "[{\"Url\":\"https://youtu.be/abcdefghijk\",\"Options\":{\"IsVideo\":false,\"Format\":\"flac\"}}]");
        await vm.InitializeAsync();
        Check(startupFixture.Checks == 1 && vm.Updates.HasUpdate && vm.Items.Single().Options is { Format: "flac" } && File.Exists(Path.Combine(settingsPath, "update-queue.json.restored")), "startup checks updates and safely restores pre-update queue options");
        Check(vm.Updates.InstallCommand.CanExecute(null), "idle application exposes the verified update install action");
        Task active = vm.StartAsync(); await Task.Delay(30);
        Check(!vm.Updates.InstallCommand.CanExecute(null), "real queue activity disables the application update install command");
        vm.CancelAll(); await active;
        var window = new MainWindow(vm) { Width = 900, Height = 660, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show(); vm.Panel = "About";
        await Capture(window, Path.Combine("artifacts", "wpf-preview", "app-update.png"));
        Check(vm.Version.Contains("4.0.1.0") && window.Icon != null, "application displays version 4.0.1.0 and the product icon");
        await vm.StopAsync(); window.Close(); await Task.Delay(80);
    }

    private sealed class AppReleaseFixture(string fault = "") : HttpMessageHandler
    {
        public static readonly byte[] Bytes = "MZ GMTFV installer fixture"u8.ToArray();
        public bool BlockDownload;
        public int Checks, Downloads;
        public TaskCompletionSource DownloadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowDownload { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.Host == "api.github.com") {
                Checks++;
                if (fault == "offline") return new(HttpStatusCode.ServiceUnavailable);
                var asset = new {
                    name = fault == "asset" ? "other.exe" : "GMTFV-4.1.0.0-setup.exe",
                    browser_download_url = fault == "foreign-url" ? "https://evil.test/tool.exe" : AppUpdateService.Repository + "/releases/download/4.1.0.0/GMTFV-4.1.0.0-setup.exe",
                    digest = fault == "digest" ? "" : "sha256:" + Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant(),
                    size = fault == "size" ? 1024L * 1024 * 1024 : Bytes.Length
                };
                string json = JsonSerializer.Serialize(new { tag_name = fault == "tag" ? "garbage" : "4.1.0.0", draft = fault == "draft", prerelease = fault == "prerelease", assets = fault == "duplicate" ? new[] { asset, asset } : new[] { asset } });
                return new(HttpStatusCode.OK) { Content = new StringContent(json) };
            }
            Downloads++; DownloadStarted.TrySetResult();
            if (BlockDownload) await AllowDownload.Task.WaitAsync(token);
            if (fault == "network") return new(HttpStatusCode.ServiceUnavailable);
            byte[] bytes = fault switch { "corrupt" => Enumerable.Repeat((byte)1, Bytes.Length).ToArray(), "truncated" => Bytes[..^1], "oversized" => Bytes.Concat(new byte[] { 0 }).ToArray(), _ => Bytes };
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        }
    }
}
