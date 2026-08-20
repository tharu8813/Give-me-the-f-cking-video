using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace GMTFV.Wpf.Services;

/// <summary>WPF 앱이 소유하는 localhost PO Token 공급자 프로세스입니다.</summary>
public sealed class PoTokenProviderService : IDisposable {
    private const int Port = 4416;
    private static readonly Uri PingUri = new("http://127.0.0.1:4416/ping");
    private readonly object syncRoot = new();
    private Process? ownedProcess;
    private Task<bool>? startupTask;

    public bool IsAvailable { get; private set; }

    public Task<bool> EnsureStartedAsync() {
        lock (syncRoot) return startupTask ??= StartAsync();
    }

    private async Task<bool> StartAsync() {
        if (await IsReachableAsync()) return IsAvailable = true;

        string providerPath = Path.Combine(AppContext.BaseDirectory, "pot-provider.exe");
        if (!File.Exists(providerPath)) return false;
        try {
            ownedProcess = Process.Start(new ProcessStartInfo {
                FileName = providerPath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList = { "--port", Port.ToString() }
            });
            for (int attempt = 0; attempt < 20; attempt++) {
                await Task.Delay(250).ConfigureAwait(false);
                if (await IsReachableAsync().ConfigureAwait(false)) return IsAvailable = true;
                if (ownedProcess is null || ownedProcess.HasExited) break;
            }
        } catch (Exception ex) {
            Debug.WriteLine("PO Token 공급자 시작 실패: " + ex.Message);
        }
        return IsAvailable = false;
    }

    private static async Task<bool> IsReachableAsync() {
        try {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            using HttpResponseMessage response = await client.GetAsync(PingUri).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        } catch { return false; }
    }

    public void ReportFailure() {
        Process? process;
        lock (syncRoot) {
            IsAvailable = false;
            process = ownedProcess;
            ownedProcess = null;
            startupTask = Task.FromResult(false);
        }
        if (process is null) return;
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        finally { process.Dispose(); }
    }

    public void Dispose() {
        IsAvailable = false;
        Process? process;
        lock (syncRoot) { process = ownedProcess; ownedProcess = null; startupTask = null; }
        if (process is null) return;
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        finally { process.Dispose(); }
    }
}
