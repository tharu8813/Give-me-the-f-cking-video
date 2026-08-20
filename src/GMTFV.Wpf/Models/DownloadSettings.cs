using GMTFV.Core;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace GMTFV.Wpf.Models;

public sealed class DownloadSettings : INotifyPropertyChanged {
    private static readonly HashSet<string> VideoContainers = new(StringComparer.OrdinalIgnoreCase) { "mp4", "mkv", "webm" };
    private static readonly HashSet<string> AudioContainers = new(StringComparer.OrdinalIgnoreCase) { "mp3", "m4a", "opus", "wav", "flac" };
    private static readonly HashSet<string> GpuAccelerators = new(StringComparer.OrdinalIgnoreCase) { "CPU", "NVIDIA", "AMD", "Intel" };
    private string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GMTFV");
    private bool isVideo = true;
    private string container = "mp4";
    private string fileNameTemplate = "%title%_%date%";
    private string gpuAccelerator = "CPU";
    private int audioBitrate = 192;
    private int maxConcurrentDownloads = 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string OutputDirectory { get => outputDirectory; set => SetField(ref outputDirectory, value); }
    public bool IsVideo { get => isVideo; set => SetField(ref isVideo, value); }
    public string Container { get => container; set => SetField(ref container, value); }
    public string FileNameTemplate { get => fileNameTemplate; set => SetField(ref fileNameTemplate, value); }
    public string GpuAccelerator { get => gpuAccelerator; set => SetField(ref gpuAccelerator, value); }
    public int AudioBitrate { get => audioBitrate; set => SetField(ref audioBitrate, value); }
    public int MaxConcurrentDownloads { get => maxConcurrentDownloads; set => SetField(ref maxConcurrentDownloads, value); }

    public DownloadSettings Clone() => new() {
        OutputDirectory = OutputDirectory,
        IsVideo = IsVideo,
        Container = Container,
        FileNameTemplate = FileNameTemplate,
        GpuAccelerator = GpuAccelerator,
        AudioBitrate = AudioBitrate,
        MaxConcurrentDownloads = MaxConcurrentDownloads
    };

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void NormalizeAndValidate() {
        OutputDirectory = (OutputDirectory ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(OutputDirectory)) throw new ArgumentException("저장 폴더를 지정해주세요.");
        try { _ = Path.GetFullPath(OutputDirectory); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { throw new ArgumentException("저장 폴더 경로가 올바르지 않습니다.", ex); }

        Container = (Container ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (!(IsVideo ? VideoContainers : AudioContainers).Contains(Container)) {
            string supported = IsVideo ? "mp4, mkv, webm" : "mp3, m4a, opus, wav, flac";
            throw new ArgumentException($"선택한 형식에서는 {supported}만 사용할 수 있습니다.");
        }

        GpuAccelerator = (GpuAccelerator ?? "CPU").Trim();
        if (!GpuAccelerators.Contains(GpuAccelerator)) throw new ArgumentException("GPU 가속 옵션이 올바르지 않습니다.");
        if (AudioBitrate is < 32 or > 512) throw new ArgumentException("오디오 비트레이트는 32~512kbps 사이여야 합니다.");
        if (MaxConcurrentDownloads is < 1 or > 5) throw new ArgumentException("동시 다운로드 수는 1~5 사이여야 합니다.");
    }

    public DownloadProfile ToProfile() => new() {
        IsVideo = IsVideo,
        Container = Container,
        Quality = string.Empty,
        FramesPerSecond = 0
    };
}
