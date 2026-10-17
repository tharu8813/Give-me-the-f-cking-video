using System.IO;
using System.Windows.Input;
using GMTFV.Core;
using GMTFV.Wpf.Commands;
using GMTFV.Wpf.Models;
using GMTFV.Wpf.Services;
namespace GMTFV.Wpf.ViewModels;

public sealed class ItemSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IDownloadBackend backend;
    private readonly PreviewSettings defaults;
    private readonly CancellationTokenSource lifetime;
    private readonly HashSet<Task> operations = [];
    public DownloadItemViewModel Item { get; }
    private bool useDefaults;
    public bool UseDefaults { get => useDefaults; set { if (Set(ref useDefaults, value)) Raise(nameof(CanEditOptions)); } }
    public bool CanEditOptions => !UseDefaults;
    private bool isVideo;
    public bool IsVideo
    {
        get => isVideo;
        set {
            if (!Set(ref isVideo, value)) return;
            Raise(nameof(Formats));
            Format = value ? "mp4" : "mp3";
        }
    }
    public string[] Formats => IsVideo ? MediaFormats.Video : MediaFormats.Audio;
    private string format;
    public string Format { get => format; set { if (value != null && Formats.Contains(value)) Set(ref format, value); } }
    public QualityChoice[] Qualities { get; }
    public QualityChoice SelectedQuality { get; set; }
    public string UploadDate => Item.Metadata?.UploadDate.ToString("yyyy-MM-dd") ?? "—";
    private IReadOnlyList<SubtitleTrack> subtitles = [];
    public IReadOnlyList<SubtitleTrack> Subtitles { get => subtitles; private set => Set(ref subtitles, value); }
    private SubtitleTrack? selectedSubtitle;
    public SubtitleTrack? SelectedSubtitle { get => selectedSubtitle; set => Set(ref selectedSubtitle, value); }
    private bool isBusy;
    public bool IsBusy { get => isBusy; private set { Set(ref isBusy, value); CommandManager.InvalidateRequerySuggested(); } }
    private string status = "자막 조회를 눌러 사용 가능한 언어를 확인하세요.";
    public string Status { get => status; private set => Set(ref status, value); }
    public ICommand LoadSubtitlesCommand { get; }
    public ICommand DownloadSubtitleCommand { get; }
    public ICommand CancelSubtitleCommand { get; }
    private CancellationTokenSource? subtitleCancellation;
    public ItemSettingsViewModel(DownloadItemViewModel item, PreviewSettings defaults, IDownloadBackend backend, CancellationToken token)
    {
        Item = item; this.defaults = defaults; this.backend = backend;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        useDefaults = item.Options == null;
        isVideo = item.Options?.IsVideo ?? defaults.IsVideo;
        format = item.Options?.Format ?? defaults.Format;
        Qualities = new[] { new QualityChoice(null, null) }.Concat((item.Metadata?.Formats ?? []).Where(f => f.Height > 0)
            .Select(f => new QualityChoice(f.Height, f.Fps > 0 ? f.Fps : null)).Distinct().OrderByDescending(f => f.Height).ThenByDescending(f => f.Fps)).ToArray();
        int? height = item.Options != null ? item.Options.Height : defaults.Height == 0 ? null : defaults.Height;
        SelectedQuality = Qualities.FirstOrDefault(q => q.Height == height && (item.Options?.Fps == null || q.Fps == item.Options.Fps)) ?? Qualities[0];
        LoadSubtitlesCommand = new AsyncCommand(_ => LoadSubtitlesAsync(), ex => Status = ex.Message, _ => !IsBusy);
        DownloadSubtitleCommand = new AsyncCommand(_ => DownloadSubtitleAsync(), ex => Status = ex.Message, _ => !IsBusy && SelectedSubtitle != null);
        CancelSubtitleCommand = new RelayCommand(_ => subtitleCancellation?.Cancel(), _ => IsBusy);
    }
    public void Apply()
    {
        var options = UseDefaults ? null : new ItemDownloadOptions { IsVideo = IsVideo, Format = Format, Height = SelectedQuality.Height, Fps = SelectedQuality.Fps };
        options?.Validate();
        Item.Options = options;
    }
    private async Task TrackAsync(Task task) { operations.Add(task); try { await task; } finally { operations.Remove(task); } }
    public Task LoadSubtitlesAsync() => TrackAsync(LoadSubtitlesCoreAsync());
    private async Task LoadSubtitlesCoreAsync()
    {
        if (IsBusy) return;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); subtitleCancellation = cts;
        IsBusy = true; Status = "자막 언어를 확인하는 중…";
        try {
            Subtitles = await backend.GetSubtitlesAsync(Item.Url, defaults.ToolFolder, cts.Token);
            SelectedSubtitle = Subtitles.FirstOrDefault();
            Status = Subtitles.Count == 0 ? "사용 가능한 자막이 없습니다." : $"자막 {Subtitles.Count}개 · 언어를 선택한 후 다운로드하세요.";
        }
        catch (OperationCanceledException) { Status = "자막 조회를 취소했습니다."; }
        finally { subtitleCancellation = null; IsBusy = false; }
    }
    public Task DownloadSubtitleAsync() => TrackAsync(DownloadSubtitleCoreAsync());
    private async Task DownloadSubtitleCoreAsync()
    {
        if (IsBusy || SelectedSubtitle == null) return;
        defaults.Validate();
        var track = SelectedSubtitle;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); subtitleCancellation = cts;
        IsBusy = true; Status = "자막을 다운로드하는 중…";
        try {
            await Task.Run(() => Directory.CreateDirectory(defaults.OutputFolder), cts.Token);
            string name = WindowsFileName.Build(Item.Title, Item.Channel, Item.Metadata!.Id, "srt", "%title%_" + track.Language, 1);
            string path = await backend.DownloadSubtitleAsync(Item.Url, track, Path.Combine(defaults.OutputFolder, name), defaults.ToolFolder, cts.Token);
            Status = "자막 저장 완료 · " + path;
        }
        catch (OperationCanceledException) { Status = "자막 다운로드를 취소했습니다."; }
        finally { subtitleCancellation = null; IsBusy = false; }
    }
    public async Task StopAsync() { lifetime.Cancel(); subtitleCancellation?.Cancel(); await Task.WhenAll(operations.ToArray()); }
    public void Dispose() => lifetime.Dispose();
}
