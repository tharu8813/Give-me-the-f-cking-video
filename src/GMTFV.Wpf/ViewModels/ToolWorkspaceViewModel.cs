using System.IO;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Services;
namespace GMTFV.Wpf.ViewModels;

public sealed class ToolWorkspaceViewModel(string root, DiagnosticLog log, CancellationToken lifetime) : ObservableObject
{
    private bool isPreparing, isChecking, missing;
    private string status = "도구 상태를 확인해 주세요", report = "설치된 버전 다시 확인을 눌러 상태를 확인하세요.";
    private CancellationTokenSource? preparation;
    public bool IsPreparing { get => isPreparing; private set => Set(ref isPreparing, value); }
    public bool IsChecking { get => isChecking; private set => Set(ref isChecking, value); }
    public bool Missing { get => missing; private set => Set(ref missing, value); }
    public string Status { get => status; private set => Set(ref status, value); }
    public string Report { get => report; private set => Set(ref report, value); }
    public void Cancel() => preparation?.Cancel();
    public async Task CheckAsync(string toolFolder)
    {
        if (IsChecking) return;
        IsChecking = true;
        try {
            Report = "설치된 도구 버전을 확인하는 중…";
            string folder = Path.GetFullPath(toolFolder);
            var versions = await Task.Run(async () => {
                var runner = new ProcessRunner();
                return await Task.WhenAll(new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }.Select(async name => {
                    string? path = name == "deno.exe" ? ToolDiagnostics.ResolveDeno(folder) : Path.Combine(folder, name);
                    if (name == "ffprobe.exe" && File.Exists(path)) {
                        try { var result = await runner.RunAsync(path, "-version", TimeSpan.FromSeconds(5), lifetime); result.EnsureSuccess(); return name + "\n" + result.Output.Split('\r', '\n')[0]; }
                        catch (Exception ex) when (ex is ToolExecutionException or TimeoutException or System.ComponentModel.Win32Exception) { return name + "\nunavailable: " + DiagnosticLog.Redact(ex.Message); }
                    }
                    return $"{name}\n{(path == null ? "missing" : await ToolDiagnostics.VersionAsync(path, runner, lifetime))}";
                }));
            }, lifetime);
            Missing = versions.Any(v => v.Contains("missing") || v.Contains("unavailable:")) || !File.Exists(Path.Combine(folder, "ffprobe.exe"));
            if (!Missing) {
                try { await Task.Run(() => ToolDiagnostics.CheckDenoAsync(folder, new ProcessRunner(), lifetime), lifetime); }
                catch (InvalidOperationException) { Missing = true; }
            }
            Status = Missing ? "다운로드 도구 준비가 필요합니다" : "다운로드 도구가 준비되었습니다";
            Report = folder + "\n\n" + string.Join("\n\n", versions);
        }
        finally { IsChecking = false; }
    }
    public async Task<string?> PrepareAsync()
    {
        if (IsPreparing) return null;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        preparation = cts; IsPreparing = true;
        try {
            var progress = new Progress<string>(text => Status = text);
            return await Task.Run(() => new ToolSetupService(root).InstallAsync(progress, cts.Token), cts.Token);
        }
        catch (OperationCanceledException) { Status = "준비를 취소했습니다. 기존 도구를 계속 사용합니다."; return null; }
        catch (Exception ex) { Status = "도구 준비 실패 · 네트워크와 저장 권한을 확인한 뒤 다시 시도하세요."; Report = DiagnosticLog.Redact(ex.Message); log.Write("tools", ex.ToString()); return null; }
        finally { preparation = null; IsPreparing = false; }
    }
}
