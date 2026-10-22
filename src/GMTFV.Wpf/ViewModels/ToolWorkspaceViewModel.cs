using System.IO;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Services;
namespace GMTFV.Wpf.ViewModels;

public sealed record ToolHealth(string Name, string Purpose, string State, string Detail, bool? Ready = null);

public sealed class ToolWorkspaceViewModel(string root, DiagnosticLog log, CancellationToken lifetime, IProcessRunner? runner = null) : ObservableObject
{
    private readonly IProcessRunner processes = runner ?? new ProcessRunner();
    private bool isPreparing, isChecking, missing, hasChecked;
    private int revision;
    private string status = "다운로드 준비 상태를 확인해 주세요", guidance = "상태를 확인하면 필요한 준비를 안내해 드립니다.", report = "아직 확인한 정보가 없습니다.", activity = "";
    private CancellationTokenSource? preparation;
    private static ToolHealth[] UncheckedTools() => [
        new("yt-dlp", "영상 내려받기", "확인 전", ""), new("FFmpeg", "영상·오디오 변환", "확인 전", ""),
        new("FFprobe", "저장 파일 확인", "확인 전", ""), new("Deno", "YouTube 영상 정보 읽기", "확인 전", "")];
    private ToolHealth[] components = UncheckedTools();
    public ToolHealth[] Components { get => components; private set => Set(ref components, value); }
    public bool IsPreparing { get => isPreparing; private set { if (Set(ref isPreparing, value)) RaiseState(); } }
    public bool IsChecking { get => isChecking; private set { if (Set(ref isChecking, value)) RaiseState(); } }
    public bool Missing { get => missing; private set { if (Set(ref missing, value)) RaiseState(); } }
    public bool HasChecked { get => hasChecked; private set { if (Set(ref hasChecked, value)) RaiseState(); } }
    public bool IsReady => HasChecked && !Missing && !IsBusy;
    public bool IsBusy => IsPreparing || IsChecking;
    public string StateLabel => IsPreparing ? "준비 중" : IsChecking ? "확인 중" : !HasChecked ? "확인 필요" : Missing ? "준비 필요" : "사용 가능";
    public string InstallLabel => HasChecked && Missing ? "한 번에 준비하기" : "필요한 도구 자동 설치";
    public string Status { get => status; private set => Set(ref status, value); }
    public string Guidance { get => guidance; private set => Set(ref guidance, value); }
    public string Activity { get => activity; private set => Set(ref activity, value); }
    public string Report { get => report; private set => Set(ref report, value); }
    private int? percent;
    public int? Percent { get => percent; private set { if (Set(ref percent, value)) { Raise(nameof(Progress)); Raise(nameof(IsIndeterminate)); } } }
    public double Progress => Percent ?? 0;
    public bool IsIndeterminate => IsChecking || Percent == null;
    private void RaiseState()
    {
        foreach (string name in new[] { nameof(IsReady), nameof(IsBusy), nameof(StateLabel), nameof(InstallLabel), nameof(IsIndeterminate) }) Raise(name);
    }
    public void Invalidate()
    {
        revision++; HasChecked = false; Missing = false; Components = UncheckedTools();
        Status = "다운로드 준비 상태를 확인해 주세요";
        Guidance = "도구 위치가 바뀌었습니다. 상태를 다시 확인해 주세요.";
        Report = "도구 위치가 바뀌어 이전 확인 결과를 지웠습니다.";
    }
    public void Cancel() => preparation?.Cancel();
    public async Task CheckAsync(string toolFolder)
    {
        if (IsBusy) return;
        int current = revision;
        IsChecking = true; Percent = null;
        Status = "다운로드 기능을 확인하고 있어요";
        Guidance = "필요한 도구가 실행되는지 확인합니다. 잠시만 기다려 주세요.";
        try {
            string folder = Path.GetFullPath(toolFolder);
            var results = await Task.Run(() => Task.WhenAll(UncheckedTools().Select(async tool => {
                string? path = tool.Name == "Deno" ? ToolDiagnostics.ResolveDeno(folder) : Path.Combine(folder, tool.Name.ToLowerInvariant() + ".exe");
                string version = path == null ? "missing" : await ToolDiagnostics.VersionAsync(path, processes, lifetime);
                bool ready = version != "missing" && !version.StartsWith("unavailable:", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(version);
                string state = ready ? "사용 가능" : version == "missing" ? "설치 필요" : "확인 필요";
                if (ready && tool.Name == "Deno") {
                    try { await ToolDiagnostics.CheckDenoAsync(folder, processes, lifetime); }
                    catch (InvalidOperationException ex) { ready = false; state = "업데이트 필요"; version += "\n" + ex.Message; }
                }
                return tool with { Ready = ready, State = state, Detail = DiagnosticLog.Redact((path ?? "설치 위치 없음") + "\n" + version) };
            })), lifetime);
            if (current != revision) return;
            Components = results; Missing = results.Any(t => t.Ready != true); HasChecked = true;
            Status = Missing ? "다운로드에 필요한 준비가 남아 있어요" : "다운로드할 준비가 되었어요";
            Guidance = Missing ? "아래에서 준비가 필요한 기능을 확인하세요. ‘한 번에 준비하기’를 누르면 필요한 도구를 자동으로 받습니다."
                : "필요한 도구가 모두 정상입니다. 영상 링크를 추가하고 다운로드를 시작하세요.";
            Report = DiagnosticLog.Redact(folder) + "\n\n" + string.Join("\n\n", results.Select(t => t.Name + " · " + t.State + "\n" + t.Detail));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) {
            if (current != revision) return;
            Missing = true; HasChecked = true;
            Status = "다운로드 준비 상태를 확인하지 못했어요";
            Guidance = "다시 확인해 주세요. 같은 문제가 계속되면 도구를 새로 받거나, 자세한 정보에서 도구 위치를 확인하세요.";
            Report = DiagnosticLog.Redact(ex.ToString()); log.Write("tools-check", ex.ToString());
        }
        finally { IsChecking = false; }
    }
    public async Task<string?> PrepareAsync()
    {
        if (IsBusy) return null;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        preparation = cts; IsPreparing = true;
        try {
            Percent = null; Status = "다운로드 기능을 준비하고 있어요";
            Guidance = "필요한 파일을 자동으로 받고 확인합니다. 처음 준비할 때는 시간이 걸릴 수 있어요.";
            Activity = "설치를 시작하고 있어요"; Report = "도구 준비를 시작했습니다.";
            var progress = new Progress<ToolSetupProgress>(value => {
                if (preparation != cts || cts.IsCancellationRequested) return;
                Percent = value.Phase == "다운로드 중" ? value.Percent : null;
                Activity = value.Phase == "다운로드 중" ? "필요한 파일을 받는 중" + (value.Percent is int p ? $" · {p}%" : "") + $" · {value.Bytes / 1048576d:F1} MB"
                    : value.Step switch { 1 => "설치할 공간을 확인하고 있어요", 2 => "배포 정보를 확인하고 있어요", 3 => "연결이 원활하지 않아 잠시 후 다시 시도해요", 4 => "받은 파일이 손상되지 않았는지 확인하고 있어요", 5 => "받은 파일을 풀고 있어요", 6 => "도구가 정상적으로 실행되는지 확인하고 있어요", _ => "설치를 마무리하고 있어요" };
                Report = DiagnosticLog.Redact(value.Text);
            });
            string folder = await Task.Run(() => new ToolSetupService(root).InstallAsync(null, cts.Token, progress), cts.Token);
            Status = "설치가 끝났어요"; Guidance = "설정에 적용한 뒤 준비 상태를 다시 확인합니다.";
            return folder;
        }
        catch (OperationCanceledException) { Status = "다운로드 준비를 취소했어요"; Guidance = "이전에 사용하던 설정은 유지됩니다. 다시 준비하거나 상태를 확인할 수 있어요."; return null; }
        catch (Exception ex) { var failure = ToolSetupDiagnostics.Classify(ex); Status = "다운로드 준비를 마치지 못했어요"; Guidance = failure.Message; Report = failure.Kind + "\n" + DiagnosticLog.Redact(ex.ToString()) + "\n로그: " + DiagnosticLog.Redact(log.DirectoryPath); log.Write("tools", ex.ToString()); return null; }
        finally { preparation = null; IsPreparing = false; Percent = null; Activity = ""; }
    }
}
