using System.Windows.Input;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Commands;
using GMTFV.Wpf.Services;

namespace GMTFV.Wpf.ViewModels;

public sealed class AppUpdateViewModel : ObservableObject
{
    private readonly AppUpdateService service;
    private readonly string directory;
    private readonly Func<bool> canInstall;
    private readonly Func<string, Task> install;
    private readonly DiagnosticLog log;
    private readonly CancellationToken lifetime;
    private CancellationTokenSource? cancellation;
    private AppRelease? available;
    private string? prepared;
    private bool isBusy;
    public bool IsBusy { get => isBusy; private set { if (Set(ref isBusy, value)) CommandManager.InvalidateRequerySuggested(); } }
    public bool HasUpdate => available != null;
    public string UpdateLabel => available == null ? "앱 정보" : $"{available.Version} 업데이트";
    private string status = "업데이트를 확인할 수 있습니다.";
    public string Status { get => status; private set => Set(ref status, value); }
    private double percent;
    public double Percent { get => percent; private set => Set(ref percent, value); }
    public ICommand CheckCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand CancelCommand { get; }

    public AppUpdateViewModel(AppUpdateService service, string directory, Func<bool> canInstall, Func<string, Task> install, DiagnosticLog log, CancellationToken lifetime)
    {
        this.service = service; this.directory = directory; this.canInstall = canInstall; this.install = install; this.log = log; this.lifetime = lifetime;
        CheckCommand = new AsyncCommand(_ => CheckAsync(), Failed, _ => !IsBusy);
        InstallCommand = new AsyncCommand(_ => InstallAsync(), Failed, _ => HasUpdate && !IsBusy && canInstall());
        CancelCommand = new RelayCommand(_ => cancellation?.Cancel(), _ => IsBusy);
    }

    public async Task CheckAsync()
    {
        if (IsBusy || lifetime.IsCancellationRequested) return;
        IsBusy = true; Percent = 0; Status = "새 버전을 확인하는 중…";
        using var source = CancellationTokenSource.CreateLinkedTokenSource(lifetime); cancellation = source;
        try {
            available = await service.CheckAsync(typeof(AppUpdateViewModel).Assembly.GetName().Version!, source.Token);
            prepared = null;
            Raise(nameof(HasUpdate)); Raise(nameof(UpdateLabel));
            Status = available == null ? "최신 버전을 사용하고 있습니다." : $"{available.Version} 버전을 설치할 수 있습니다.";
        }
        catch (OperationCanceledException) { Status = source.IsCancellationRequested ? "업데이트 확인을 취소했습니다." : "확인 시간이 초과되었습니다. 잠시 후 다시 확인하세요."; }
        catch (Exception ex) { Failed(ex); }
        finally { cancellation = null; IsBusy = false; }
    }

    public async Task InstallAsync()
    {
        if (IsBusy || available == null || !canInstall() || lifetime.IsCancellationRequested) return;
        IsBusy = true; Percent = 0; Status = "설치 파일을 다운로드하고 검증하는 중…";
        using var source = CancellationTokenSource.CreateLinkedTokenSource(lifetime); cancellation = source;
        try {
            prepared ??= await service.DownloadAsync(available, directory, new Progress<double>(value => { if (ReferenceEquals(cancellation, source) && !source.IsCancellationRequested) Percent = value; }), source.Token);
            await AppUpdateService.VerifyAsync(available, prepared, source.Token);
            source.Token.ThrowIfCancellationRequested();
            if (!canInstall()) { Status = "진행 중인 작업을 마친 뒤 업데이트 설치를 다시 눌러주세요."; return; }
            Status = "검증 완료 · 설치 프로그램을 여는 중…";
            await install(prepared);
        }
        catch (OperationCanceledException) { Status = source.IsCancellationRequested ? "업데이트 다운로드를 취소했습니다." : "다운로드 시간이 초과되었습니다. 다시 시도하세요."; }
        catch (Exception ex) { prepared = null; Failed(ex); }
        finally { cancellation = null; IsBusy = false; }
    }

    public async Task StopAsync()
    {
        cancellation?.Cancel();
        while (IsBusy) await Task.Delay(20);
    }
    private void Failed(Exception ex)
    {
        log.Write("app-update", ex.ToString());
        Status = ex is System.Net.Http.HttpRequestException ? "업데이트 서버에 연결하지 못했습니다. 네트워크를 확인하고 다시 시도하세요." : "업데이트를 완료하지 못했습니다. " + DiagnosticLog.Redact(ex.Message);
    }
}
