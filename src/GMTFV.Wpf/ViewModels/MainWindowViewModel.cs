using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GMTFV.Core;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Commands;
using GMTFV.Wpf.Models;
using GMTFV.Wpf.Services;
using Microsoft.Win32;

namespace GMTFV.Wpf.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IDownloadBackend backend;
    private readonly DiagnosticLog log;
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim metadataSlots = new(3);
    private readonly HashSet<Task> operations = [];
    private bool disposed;
    private bool summaryPending;
    private int toastSequence;
    public ObservableCollection<DownloadItemViewModel> Items { get; } = [];
    public PreviewSettings Settings { get; }
    public bool ReduceMotion { get => Settings.ReduceMotion; set { Settings.ReduceMotion = value; Motion.ReduceMotion = value; Raise(); } }
    public string[] Formats => Settings.IsVideo ? MediaFormats.Video : MediaFormats.Audio;
    public string? SelectedFormat
    {
        get => Settings.Format;
        set {
            // ComboBox clears selection while its video/audio ItemsSource is changing.
            if (value == null || value == Settings.Format || !Formats.Contains(value)) return;
            Settings.Format = value; Raise(); RefreshSelections();
        }
    }
    public int[] Heights { get; } = [0, 2160, 1440, 1080, 720, 480, 360];
    public int[] ConcurrencyChoices { get; } = Enumerable.Range(1, 16).ToArray();
    public string[] Encoders { get; } = ["libx264", "h264_nvenc", "h264_amf", "h264_qsv"];
    public int[] AudioBitrates => new[] { 32, 64, 96, 128, 160, 192, 256, 320, 384, 448, 512, Settings.AudioBitrate }.Distinct().Order().ToArray();
    private string urlInput = "";
    public string UrlInput { get => urlInput; set => Set(ref urlInput, value); }
    private string toast = "";
    public string Toast { get => toast; private set => Set(ref toast, value); }
    private string panel = "";
    public string Panel
    {
        get => panel;
        set {
            if (value.Length > 0) { DisplayedPanel = value; Raise(nameof(DisplayedPanel)); Raise(nameof(PanelTitle)); }
            if (Set(ref panel, value)) { Raise(nameof(HasPanel)); Raise(nameof(IsWorkspaceEnabled)); }
        }
    }
    public string DisplayedPanel { get; private set; } = "";
    public string PanelTitle => DisplayedPanel switch { "Settings" => "설정", "Diagnostics" => "도구 및 진단", "About" => "앱 정보", _ => "" };
    public bool HasPanel => Panel.Length > 0;
    public bool IsWorkspaceEnabled => !HasPanel && Failure == null && ItemSettings == null;
    private ItemSettingsViewModel? itemSettings;
    public ItemSettingsViewModel? ItemSettings { get => itemSettings; private set { if (value != null) { DisplayedItemSettings = value; Raise(nameof(DisplayedItemSettings)); } if (Set(ref itemSettings, value)) Raise(nameof(IsWorkspaceEnabled)); } }
    public ItemSettingsViewModel? DisplayedItemSettings { get; private set; }
    private string toolReport = "도구 버전 확인을 눌러 설치 경로와 버전을 확인하세요.";
    public string ToolReport { get => toolReport; private set => Set(ref toolReport, value); }
    private DownloadItemViewModel? failure;
    public DownloadItemViewModel? Failure { get => failure; set { if (Set(ref failure, value)) Raise(nameof(IsWorkspaceEnabled)); } }
    private bool isRunning;
    public bool IsRunning { get => isRunning; private set { if (Set(ref isRunning, value)) Raise(nameof(CanEditSettings)); } }
    public bool CanEditSettings => !IsRunning;
    public int CompletedCount => Items.Count(i => i.State == DownloadState.Completed);
    public int FailedCount => Items.Count(i => i.State == DownloadState.Failed);
    public int ActiveCount => Items.Count(i => i.CanCancel);
    public bool IsEmpty => Items.Count == 0;
    public double OverallProgress => Items.Count == 0 ? 0 : Items.Average(i => i.Progress);
    public string Summary => $"{Items.Count}개 항목  /  {ActiveCount}개 작업 중  /  {CompletedCount}개 완료";
    public string Version => $"GMTFV {typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3)} · .NET 10 / WPF";

    public ICommand AddCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand CancelAllCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand RetryFailedCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand PanelCommand { get; }
    public ICommand FailureCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ModeChangedCommand { get; }
    public ICommand ChooseOutputCommand { get; }
    public ICommand ChooseToolsCommand { get; }
    public ICommand OpenOutputCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand CheckToolsCommand { get; }
    public ICommand SaveListCommand { get; }
    public ICommand LoadListCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand SaveItemCommand { get; }
    public ICommand CloseItemCommand { get; }
    public ICommand CopyItemUrlCommand { get; }
    public ICommand OpenItemUrlCommand { get; }

    public MainWindowViewModel(IDownloadBackend backend, SettingsStore store, DiagnosticLog log)
    {
        this.backend = backend; this.log = log;
        Items.CollectionChanged += ItemsChanged;
        try { Settings = store.Load(); }
        catch (Exception ex) { Settings = new PreviewSettings(); Toast = "설정 로드 실패 · " + ex.Message; log.Write("settings", ex.Message); }
        AddCommand = Async(_ => { string input = UrlInput; UrlInput = ""; return AddUrlsAsync(input); }, _ => !string.IsNullOrWhiteSpace(UrlInput));
        StartCommand = Async(_ => StartAsync(), _ => !IsRunning && Items.Any(i => i.State == DownloadState.Ready));
        CancelAllCommand = new RelayCommand(_ => CancelAll(), _ => ActiveCount > 0);
        CancelCommand = new RelayCommand(p => Cancel((DownloadItemViewModel)p!), p => p is DownloadItemViewModel i && i.CanCancel);
        RetryCommand = Async(p => RetryAsync((DownloadItemViewModel)p!), p => !IsRunning && p is DownloadItemViewModel i && i.CanRetry && !i.CanCancel);
        RetryFailedCommand = Async(_ => RetryFailedAsync(), _ => !IsRunning && FailedCount > 0);
        RemoveCommand = Async(async p => {
            var item = (DownloadItemViewModel)p!;
            item.IsRemoving = true;
            if (!Settings.ReduceMotion && SystemParameters.ClientAreaAnimation) await Task.Delay(160, lifetime.Token);
            Items.Remove(item); RefreshSummary();
        }, p => p is DownloadItemViewModel i && i.Cancellation == null && !i.IsRemoving);
        PanelCommand = new RelayCommand(p => Panel = Panel == (string?)p ? "" : (string?)p ?? "");
        FailureCommand = new RelayCommand(p => Failure = p as DownloadItemViewModel);
        SaveSettingsCommand = Async(_ => { store.Save(Settings); Motion.ReduceMotion = Settings.ReduceMotion; Notify("설정이 저장되었습니다."); RefreshSelections(); return Task.CompletedTask; });
        ModeChangedCommand = new RelayCommand(_ => { Raise(nameof(Settings)); Raise(nameof(Formats)); Settings.Format = Settings.IsVideo ? "mp4" : "mp3"; Raise(nameof(SelectedFormat)); RefreshSelections(); });
        ChooseOutputCommand = Async(_ => { PickFolder(false); return Task.CompletedTask; }, _ => !IsRunning);
        ChooseToolsCommand = Async(_ => { PickFolder(true); return Task.CompletedTask; }, _ => !IsRunning);
        OpenOutputCommand = Async(_ => { OpenFolder(Settings.OutputFolder); return Task.CompletedTask; });
        OpenLogsCommand = Async(_ => { OpenFolder(log.DirectoryPath); return Task.CompletedTask; });
        CheckToolsCommand = Async(_ => CheckToolsAsync());
        SaveListCommand = Async(_ => SaveListAsync());
        LoadListCommand = Async(_ => LoadListAsync());
        EditItemCommand = new RelayCommand(p => ItemSettings = new ItemSettingsViewModel((DownloadItemViewModel)p!, Settings, backend, lifetime.Token),
            p => !IsRunning && ItemSettings == null && p is DownloadItemViewModel { Metadata: not null, CanCancel: false, IsRemoving: false });
        SaveItemCommand = Async(async _ => { ItemSettings!.Apply(); RefreshSelections(); await CloseItemSettingsAsync(); }, _ => ItemSettings != null && !ItemSettings.IsBusy);
        CloseItemCommand = Async(_ => CloseItemSettingsAsync());
        CopyItemUrlCommand = Async(_ => { Clipboard.SetText(ItemSettings!.Item.Url); Notify("영상 주소를 복사했습니다."); return Task.CompletedTask; }, _ => ItemSettings != null);
        OpenItemUrlCommand = Async(_ => { Process.Start(new ProcessStartInfo(ItemSettings!.Item.Url) { UseShellExecute = true }); return Task.CompletedTask; }, _ => ItemSettings != null);
    }

    private AsyncCommand Async(Func<object?, Task> action, Func<object?, bool>? can = null) => new(p => TrackAsync(action(p)), ReportError, can);
    private async Task TrackAsync(Task task)
    {
        operations.Add(task);
        try { await task; } finally { operations.Remove(task); }
    }
    public Task AddUrlsAsync(string input) => TrackAsync(AddUrlsCoreAsync(input));
    private async Task AddUrlsCoreAsync(string input, IReadOnlyDictionary<string, ItemDownloadOptions?>? options = null)
    {
        var added = new List<DownloadItemViewModel>();
        var urls = Items.Select(i => i.Url).ToHashSet(StringComparer.Ordinal);
        int invalid = 0, duplicate = 0;
        foreach (string value in Regex.Split(input.Trim(), @"[\s,]+"))
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            string url;
            try { url = YouTubeUrl.Normalize(value); } catch (ArgumentException) { invalid++; continue; }
            if (!urls.Add(url)) { duplicate++; continue; }
            var item = new DownloadItemViewModel(url) { Options = options != null && options.TryGetValue(url, out var saved) ? saved : null };
            item.Selection = SelectionText(item: item);
            Items.Add(item); added.Add(item);
        }
        RefreshSummary();
        Notify($"{added.Count}개 추가 · 중복 {duplicate}개 · 잘못된 주소 {invalid}개");
        await Task.WhenAll(added.Select(LoadMetadataAsync));
    }
    private async Task LoadMetadataAsync(DownloadItemViewModel item)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        item.Cancellation = cts; item.Raise(nameof(DownloadItemViewModel.CanCancel)); item.State = DownloadState.Metadata;
        item.Error = ""; item.Message = "메타데이터 조회 중";
        bool acquired = false;
        try
        {
            await metadataSlots.WaitAsync(cts.Token); acquired = true;
            var data = await backend.GetMetadataAsync(item.Url, Settings.ToolFolder, cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            item.Metadata = data; item.Title = data.Title; item.Channel = data.Uploader;
            item.Duration = data.Duration.TotalHours >= 1 ? data.Duration.ToString(@"h\:mm\:ss") : data.Duration.ToString(@"m\:ss");
            item.Thumbnail = $"https://i.ytimg.com/vi/{data.Id}/mqdefault.jpg";
            item.Selection = SelectionText(item: item); item.State = DownloadState.Ready; item.Message = "다운로드 준비 완료";
        }
        catch (OperationCanceledException) { item.State = DownloadState.Cancelled; item.Message = "정보 조회가 취소되었습니다."; }
        catch (Exception ex) { Fail(item, ex); }
        finally { if (acquired) metadataSlots.Release(); item.Cancellation = null; item.Raise(nameof(DownloadItemViewModel.CanCancel)); item.Raise(nameof(DownloadItemViewModel.IsIndeterminate)); RefreshSummary(); }
    }
    public Task StartAsync() => StartItemsAsync(Items.Where(i => i.State == DownloadState.Ready && !i.IsRemoving).ToArray());
    private async Task StartItemsAsync(DownloadItemViewModel[] ready)
    {
        if (IsRunning) return;
        Settings.Validate();
        if (ready.Length == 0) return;
        var snapshot = JsonSerializer.Deserialize<PreviewSettings>(JsonSerializer.Serialize(Settings))!;
        IsRunning = true;
        try
        {
            await Task.Run(() => Directory.CreateDirectory(snapshot.OutputFolder), lifetime.Token);
            using var queue = new DownloadQueueService(snapshot.Concurrency);
            await Task.WhenAll(ready.Select((item, index) => DownloadAsync(item, snapshot, queue, index + 1)));
            Notify($"작업 종료 · 완료 {CompletedCount}개 · 실패 {FailedCount}개");
        }
        finally { IsRunning = false; RefreshSummary(); }
    }
    private async Task DownloadAsync(DownloadItemViewModel item, PreviewSettings options, DownloadQueueService queue, int number)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        item.Cancellation = cts; item.Raise(nameof(DownloadItemViewModel.CanCancel));
        item.State = DownloadState.Queued; item.Percent = null; item.Error = ""; item.Selection = SelectionText(options, item); item.Message = "다운로드 슬롯 대기 중";
        int? slot = null;
        try
        {
            slot = await queue.AcquireSlotAsync(cts.Token);
            var data = item.Metadata!;
            var selected = item.Options;
            selected?.Validate();
            string extension = selected?.Format ?? options.Format;
            var request = new DownloadRequest {
                Url = item.Url, OutputPath = Path.Combine(options.OutputFolder, WindowsFileName.Build(data.Title, data.Uploader, data.Id, extension, options.FileNameTemplate, number)),
                IsVideo = selected?.IsVideo ?? options.IsVideo, Extension = extension, Height = selected != null ? selected.Height : options.Height == 0 ? null : options.Height, Fps = selected?.Fps,
                AudioBitrate = options.AudioBitrate, AllowQualityFallback = options.AllowQualityFallback, VideoEncoder = options.VideoEncoder
            };
            var progress = new Progress<DownloadProgress>(p => {
                if (item.Cancellation != cts || cts.IsCancellationRequested) return;
                item.State = p.State; item.Percent = p.Percent; item.Message = p.Message;
            });
            item.OutputPath = await backend.DownloadAsync(request, options.ToolFolder, progress, cts.Token);
            item.State = DownloadState.Completed; item.Percent = 100; item.Message = "저장 완료 · " + Path.GetFileName(item.OutputPath);
        }
        catch (OperationCanceledException) { item.State = DownloadState.Cancelled; item.Message = "취소됨 · 임시 파일 보존"; }
        catch (Exception ex) { Fail(item, ex); }
        finally { if (slot.HasValue) queue.ReleaseSlot(slot.Value); item.Cancellation = null; item.Raise(nameof(DownloadItemViewModel.CanCancel)); item.Raise(nameof(DownloadItemViewModel.IsIndeterminate)); RefreshSummary(); }
    }
    public async Task RetryAsync(DownloadItemViewModel item)
    {
        if (IsRunning || item.CanCancel) return;
        item.Error = ""; item.Percent = null;
        // Metadata failures must be queried again before a download request can be built.
        if (item.Metadata == null) { await LoadMetadataAsync(item); return; }
        Settings.Validate(); IsRunning = true;
        try { using var queue = new DownloadQueueService(1); await DownloadAsync(item, JsonSerializer.Deserialize<PreviewSettings>(JsonSerializer.Serialize(Settings))!, queue, Items.IndexOf(item) + 1); }
        finally { IsRunning = false; }
    }
    public async Task RetryFailedAsync()
    {
        if (IsRunning) return;
        var failed = Items.Where(i => i.State == DownloadState.Failed).ToArray();
        await Task.WhenAll(failed.Where(i => i.Metadata == null).Select(LoadMetadataAsync));
        if (IsRunning) return;
        var ready = failed.Where(i => i.Metadata != null && !i.CanCancel && !i.IsRemoving && i.State is DownloadState.Failed or DownloadState.Ready).ToArray();
        await StartItemsAsync(ready);
    }
    private static void Cancel(DownloadItemViewModel item) { item.Cancellation?.Cancel(); item.Raise(nameof(DownloadItemViewModel.CanCancel)); }
    public void CancelAll() { foreach (var item in Items) Cancel(item); RefreshSummary(); }
    private void Fail(DownloadItemViewModel item, Exception ex)
    {
        item.State = DownloadState.Failed;
        item.Error = RecoveryPolicy.Message(RecoveryPolicy.Classify(ex.InnerException ?? ex)) + "\n\n" + DiagnosticLog.Redact(ex.Message);
        item.Message = item.Error.Split('\n')[0]; log.Write("preview", item.Error);
    }
    public void ReportError(Exception ex) { log.Write("preview", ex.ToString()); Notify(ex.Message); }
    public async void Notify(string message)
    {
        int sequence = ++toastSequence; Toast = message;
        try { await Task.Delay(6500, lifetime.Token); if (sequence == toastSequence) Toast = ""; }
        catch (OperationCanceledException) { }
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(DownloadItemViewModel.State) or nameof(DownloadItemViewModel.Percent) or nameof(DownloadItemViewModel.CanCancel)) RefreshSummary(); }
    private void ItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null) foreach (DownloadItemViewModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
        if (e.NewItems != null) foreach (DownloadItemViewModel item in e.NewItems) item.PropertyChanged += ItemChanged;
        RefreshSummary();
    }
    private void RefreshSummary()
    {
        if (summaryPending || disposed) return;
        summaryPending = true;
        // Coalesce row changes so a large import does not repeatedly scan the queue.
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () => {
            summaryPending = false;
            if (disposed) return;
            foreach (string name in new[] { nameof(CompletedCount), nameof(FailedCount), nameof(ActiveCount), nameof(IsEmpty), nameof(Summary), nameof(OverallProgress) }) Raise(name);
            CommandManager.InvalidateRequerySuggested();
        });
    }
    private string SelectionText(PreviewSettings? options = null, DownloadItemViewModel? item = null)
    {
        var s = options ?? Settings; var o = item?.Options;
        bool video = o?.IsVideo ?? s.IsVideo;
        int? height = o != null ? o.Height : s.Height == 0 ? null : s.Height;
        return $"{(o?.Format ?? s.Format).ToUpperInvariant()} · {(video ? height == null ? "최고 화질" : height + "p" : s.AudioBitrate + " kbps")}" + (video && o?.Fps > 0 ? $" / {o.Fps} FPS" : "") + (o == null ? " · 기본값" : "");
    }
    public void RefreshSelections() { foreach (var i in Items.Where(i => i.State != DownloadState.Completed && !i.CanCancel)) i.Selection = SelectionText(item: i); }
    private async Task CloseItemSettingsAsync()
    {
        var current = ItemSettings; ItemSettings = null;
        if (current == null) return;
        try { await current.StopAsync(); } finally { current.Dispose(); }
    }
    private void PickFolder(bool tools)
    {
        var picker = new OpenFolderDialog { Title = tools ? "yt-dlp / FFmpeg 도구 폴더" : "다운로드 저장 폴더" };
        if (picker.ShowDialog() != true) return;
        if (tools) Settings.ToolFolder = picker.FolderName; else Settings.OutputFolder = picker.FolderName;
        Raise(nameof(Settings));
    }
    private static void OpenFolder(string path) { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
    public async Task CheckToolsAsync()
    {
        ToolReport = "도구 버전을 확인하는 중…";
        var runner = new ProcessRunner();
        string folder = Path.GetFullPath(Settings.ToolFolder);
        var versions = await Task.WhenAll(new[] { "yt-dlp.exe", "ffmpeg.exe", "deno.exe" }.Select(async name => {
            string? path = name == "deno.exe" ? ToolDiagnostics.ResolveDeno(folder) : Path.Combine(folder, name);
            return $"{name}\n{(path == null ? "missing" : await ToolDiagnostics.VersionAsync(path, runner, lifetime.Token))}";
        }));
        ToolReport = folder + "\n\n" + string.Join("\n\n", versions) + "\n\nffprobe.exe · " + (File.Exists(Path.Combine(folder, "ffprobe.exe")) ? "설치됨" : "missing") + "\n\n안정판에서 도구를 업데이트한 후 다시 확인하세요.";
    }
    private async Task SaveListAsync()
    {
        var picker = new SaveFileDialog { Filter = "URL 목록 (*.json)|*.json|텍스트 (*.txt)|*.txt", FileName = "GMTFV-queue" };
        if (picker.ShowDialog() != true) return;
        await SaveListAsync(picker.FileName);
    }
    public async Task SaveListAsync(string path)
    {
        string text = path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? string.Join(Environment.NewLine, Items.Select(i => i.Url)) : JsonSerializer.Serialize(Items.Select(i => new { i.Url, i.Options }), new JsonSerializerOptions { WriteIndented = true });
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            await File.WriteAllTextAsync(temp, text, lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        Notify("URL 목록이 저장되었습니다.");
    }
    private async Task LoadListAsync()
    {
        var picker = new OpenFileDialog { Filter = "URL 목록 (*.json;*.txt)|*.json;*.txt" };
        if (picker.ShowDialog() != true) return;
        await LoadListAsync(picker.FileName);
    }
    public Task LoadListAsync(string path) => TrackAsync(LoadListCoreAsync(path));
    private async Task LoadListCoreAsync(string path)
    {
        if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new InvalidDataException("목록 파일은 2MB 이하로 선택하세요.");
        string text = await File.ReadAllTextAsync(path, lifetime.Token);
        var options = new Dictionary<string, ItemDownloadOptions?>();
        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
            using var doc = JsonDocument.Parse(text);
            var urls = new List<string>();
            foreach (var entry in doc.RootElement.EnumerateArray()) {
                string url = (entry.ValueKind == JsonValueKind.String ? entry.GetString() : entry.GetProperty("Url").GetString()) ?? "";
                ItemDownloadOptions? value = null;
                if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("Options", out var saved) && saved.ValueKind != JsonValueKind.Null)
                    value = saved.Deserialize<ItemDownloadOptions>() ?? throw new InvalidDataException("영상별 설정이 비어 있습니다.");
                else if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("IsTypeVideo", out var legacyMode)) {
                    string quality = entry.TryGetProperty("Quality", out var q) ? q.GetString() ?? "" : "";
                    int? height = int.TryParse(quality.TrimEnd('p'), out int h) && h > 0 ? h : null;
                    value = new ItemDownloadOptions { IsVideo = legacyMode.GetBoolean(), Format = entry.GetProperty("SubType").GetString() ?? "",
                        Height = height, Fps = height != null && entry.TryGetProperty("Fps", out var fps) && fps.GetInt32() > 0 ? fps.GetInt32() : null };
                }
                value?.Validate();
                try { options.TryAdd(YouTubeUrl.Normalize(url), value); } catch (ArgumentException) { }
                urls.Add(url);
            }
            text = string.Join("\n", urls);
        }
        await AddUrlsCoreAsync(text, options);
    }
    public async Task StopAsync()
    {
        lifetime.Cancel(); CancelAll();
        await CloseItemSettingsAsync();
        await Task.WhenAll(operations.ToArray());
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; lifetime.Cancel();
        Items.CollectionChanged -= ItemsChanged;
        foreach (var item in Items) item.PropertyChanged -= ItemChanged;
        lifetime.Dispose(); metadataSlots.Dispose();
    }
}
