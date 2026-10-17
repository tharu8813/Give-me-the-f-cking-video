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
    private readonly SettingsStore store;
    private string savedSettings = "";
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
    public string[] Modes { get; } = ["영상", "오디오"];
    public string SelectedMode { get => Settings.IsVideo ? "영상" : "오디오"; set => Settings.IsVideo = value == "영상"; }
    public int SelectedHeight { get => Settings.Height; set => Settings.Height = value; }
    public string SettingsStatus => JsonSerializer.Serialize(Settings) == savedSettings ? "설정 저장됨" : "변경됨 · 저장하면 다음 실행에도 유지됩니다";
    public string FileNamePreview => WindowsFileName.Build("나의 영상", "채널", "abcdefghijk", Settings.Format, Settings.FileNameTemplate, 1);
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
    public bool IsWorkspaceEnabled => !HasPanel && Failure == null && ItemSettings == null && !Tools.IsPreparing && !applyingUpdate;
    private ItemSettingsViewModel? itemSettings;
    public ItemSettingsViewModel? ItemSettings { get => itemSettings; private set { if (value != null) { DisplayedItemSettings = value; Raise(nameof(DisplayedItemSettings)); } if (Set(ref itemSettings, value)) Raise(nameof(IsWorkspaceEnabled)); } }
    public ItemSettingsViewModel? DisplayedItemSettings { get; private set; }
    private DownloadItemViewModel? failure;
    public DownloadItemViewModel? Failure { get => failure; set { if (Set(ref failure, value)) Raise(nameof(IsWorkspaceEnabled)); } }
    private bool isRunning;
    public bool IsRunning { get => isRunning; private set { if (Set(ref isRunning, value)) Raise(nameof(CanEditSettings)); } }
    private bool applyingUpdate;
    public bool CanEditSettings => !IsRunning && !Tools.IsPreparing && !applyingUpdate;
    public ToolWorkspaceViewModel Tools { get; }
    public AppUpdateViewModel Updates { get; }
    public int CompletedCount => Items.Count(i => i.State == DownloadState.Completed);
    public int FailedCount => Items.Count(i => i.State == DownloadState.Failed);
    public int ActiveCount => Items.Count(i => i.CanCancel);
    public bool IsEmpty => Items.Count == 0;
    public double OverallProgress => Items.Count == 0 ? 0 : 100d * CompletedCount / Items.Count;
    public string CompletionSummary => $"{CompletedCount} / {Items.Count}개 저장 완료";
    public string Summary => $"{Items.Count}개 항목  /  {ActiveCount}개 작업 중  /  {CompletedCount}개 완료";
    public string Version => $"GMTFV {typeof(MainWindowViewModel).Assembly.GetName().Version} · .NET 10 / WPF";

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
    public ICommand OpenFileCommand { get; }
    public ICommand RevealFileCommand { get; }
    public ICommand RemoveSelectedCommand { get; }
    public ICommand CancelSelectedCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand InstallToolsCommand { get; }
    public ICommand CancelToolSetupCommand { get; }
    public ICommand OpenReleaseCommand { get; }

    public MainWindowViewModel(IDownloadBackend backend, SettingsStore store, DiagnosticLog log, AppUpdateService? appUpdates = null)
    {
        this.backend = backend; this.log = log; this.store = store;
        Items.CollectionChanged += ItemsChanged;
        try { Settings = store.Load(); }
        catch (Exception ex) { Settings = new PreviewSettings(); Toast = "설정 로드 실패 · " + ex.Message; log.Write("settings", ex.Message); }
        Tools = new ToolWorkspaceViewModel(Path.Combine(store.DirectoryPath, "Tools"), log, lifetime.Token);
        Tools.PropertyChanged += ToolsChanged;
        Updates = new AppUpdateViewModel(appUpdates ?? new AppUpdateService(), Path.Combine(store.DirectoryPath, "Updates"), CanInstallAppUpdate, InstallAppUpdateAsync, log, lifetime.Token);
        savedSettings = JsonSerializer.Serialize(Settings);
        Settings.PropertyChanged += SettingsChanged;
        AddCommand = Async(_ => { string input = UrlInput; UrlInput = ""; return AddUrlsAsync(input); }, _ => !applyingUpdate && !string.IsNullOrWhiteSpace(UrlInput));
        StartCommand = Async(_ => StartAsync(), _ => CanEditSettings && Items.Any(i => i.State == DownloadState.Ready));
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
        OpenReleaseCommand = Async(_ => { Process.Start(new ProcessStartInfo(AppUpdateService.Repository + "/releases/latest") { UseShellExecute = true }); return Task.CompletedTask; });
        FailureCommand = new RelayCommand(p => Failure = p as DownloadItemViewModel);
        SaveSettingsCommand = Async(_ => { store.Save(Settings); savedSettings = JsonSerializer.Serialize(Settings); Raise(nameof(SettingsStatus)); Notify("설정이 저장되었습니다."); return Task.CompletedTask; });
        ModeChangedCommand = new RelayCommand(_ => RefreshSelections());
        ResetSettingsCommand = new RelayCommand(_ => { var defaults = new PreviewSettings(); foreach (var p in typeof(PreviewSettings).GetProperties().Where(p => p.CanWrite)) p.SetValue(Settings, p.GetValue(defaults)); Notify("기본값으로 변경했습니다. 저장하면 다음 실행에도 적용됩니다."); }, _ => CanEditSettings);
        ChooseOutputCommand = Async(_ => { PickFolder(false); return Task.CompletedTask; }, _ => !IsRunning);
        ChooseToolsCommand = Async(_ => { PickFolder(true); return Task.CompletedTask; }, _ => !IsRunning);
        OpenOutputCommand = Async(_ => { OpenFolder(Settings.OutputFolder); return Task.CompletedTask; });
        OpenLogsCommand = Async(_ => { OpenFolder(log.DirectoryPath); return Task.CompletedTask; });
        CheckToolsCommand = Async(_ => Tools.CheckAsync(Settings.ToolFolder), _ => !applyingUpdate && !Tools.IsPreparing && !Tools.IsChecking);
        InstallToolsCommand = Async(_ => InstallToolsAsync(), _ => CanEditSettings && !Tools.IsChecking && ActiveCount == 0 && ItemSettings == null);
        CancelToolSetupCommand = new RelayCommand(_ => Tools.Cancel(), _ => Tools.IsPreparing);
        SaveListCommand = Async(_ => SaveListAsync());
        LoadListCommand = Async(_ => LoadListAsync());
        EditItemCommand = new RelayCommand(p => ItemSettings = new ItemSettingsViewModel((DownloadItemViewModel)p!, Settings, backend, lifetime.Token),
            p => !IsRunning && ItemSettings == null && p is DownloadItemViewModel { Metadata: not null, CanCancel: false, IsRemoving: false });
        SaveItemCommand = Async(async _ => { ItemSettings!.Apply(); RefreshSelections(); await CloseItemSettingsAsync(); }, _ => ItemSettings != null && !ItemSettings.IsBusy);
        CloseItemCommand = Async(_ => CloseItemSettingsAsync());
        CopyItemUrlCommand = Async(_ => { Clipboard.SetText(ItemSettings!.Item.Url); Notify("영상 주소를 복사했습니다."); return Task.CompletedTask; }, _ => ItemSettings != null);
        OpenItemUrlCommand = Async(_ => { Process.Start(new ProcessStartInfo(ItemSettings!.Item.Url) { UseShellExecute = true }); return Task.CompletedTask; }, _ => ItemSettings != null);
        OpenFileCommand = Async(p => { OpenCompletedFile((DownloadItemViewModel)p!, false); return Task.CompletedTask; }, p => p is DownloadItemViewModel { HasOutput: true });
        RevealFileCommand = Async(p => { OpenCompletedFile((DownloadItemViewModel)p!, true); return Task.CompletedTask; }, p => p is DownloadItemViewModel { HasOutput: true });
        RemoveSelectedCommand = Async(async p => { var selected = Selected(p).Where(i => RemoveCommand.CanExecute(i)).ToArray(); foreach (var i in selected) i.IsRemoving = true; if (Motion.Enabled) await Task.Delay(160, lifetime.Token); foreach (var i in selected) Items.Remove(i); }, p => Selected(p).Any(i => RemoveCommand.CanExecute(i)));
        CancelSelectedCommand = new RelayCommand(p => { foreach (var item in Selected(p)) Cancel(item); });
        MoveUpCommand = new RelayCommand(p => MoveSelected(p, -1), p => CanMoveSelected(p, -1));
        MoveDownCommand = new RelayCommand(p => MoveSelected(p, 1), p => CanMoveSelected(p, 1));
    }

    private static DownloadItemViewModel[] Selected(object? value) => value is System.Collections.IList list ? list.Cast<DownloadItemViewModel>().ToArray() : [];
    private bool CanMoveSelected(object? value, int direction)
    {
        if (IsRunning || Tools.IsPreparing || Items.Any(i => i.IsRemoving)) return false;
        var selected = Selected(value).ToHashSet();
        for (int i = 0; i < Items.Count; i++) {
            int next = i + direction;
            if (next >= 0 && next < Items.Count && selected.Contains(Items[i]) && !selected.Contains(Items[next])) return true;
        }
        return false;
    }
    private void MoveSelected(object? value, int direction)
    {
        if (!CanMoveSelected(value, direction)) return;
        var selected = Selected(value).ToHashSet();
        for (int i = direction < 0 ? 1 : Items.Count - 2; i >= 0 && i < Items.Count; i -= direction) {
            int next = i + direction;
            if (next >= 0 && next < Items.Count && selected.Contains(Items[i]) && !selected.Contains(Items[next])) Items.Move(i, next);
        }
        CommandManager.InvalidateRequerySuggested();
    }
    private static void OpenCompletedFile(DownloadItemViewModel item, bool reveal)
    {
        if (!File.Exists(item.OutputPath)) throw new FileNotFoundException("저장 파일을 찾을 수 없습니다. 이동하거나 삭제했는지 확인하세요.");
        var info = new ProcessStartInfo(reveal ? "explorer.exe" : item.OutputPath!) { UseShellExecute = true };
        if (reveal) info.ArgumentList.Add("/select," + item.OutputPath);
        Process.Start(info);
    }
    private void SettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreviewSettings.IsVideo)) {
            Raise(nameof(Formats));
            if (!Formats.Contains(Settings.Format)) Settings.Format = Settings.IsVideo ? "mp4" : "mp3";
        }
        if (e.PropertyName == nameof(PreviewSettings.ReduceMotion)) Motion.ReduceMotion = Settings.ReduceMotion;
        foreach (string name in new[] { nameof(SelectedMode), nameof(SelectedFormat), nameof(SelectedHeight), nameof(SettingsStatus), nameof(FileNamePreview) }) Raise(name);
        if (e.PropertyName is nameof(PreviewSettings.IsVideo) or nameof(PreviewSettings.Format) or nameof(PreviewSettings.Height) or nameof(PreviewSettings.AudioBitrate)) RefreshSelections();
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
        if (IsRunning || Tools.IsPreparing) return;
        Settings.Validate();
        if (ready.Length == 0) return;
        var snapshot = JsonSerializer.Deserialize<PreviewSettings>(JsonSerializer.Serialize(Settings))!;
        Toast = "";
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
        Toast = "";
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
        item.ErrorKind = RecoveryPolicy.Classify(ex.InnerException ?? ex);
        item.Error = RecoveryPolicy.Message(item.ErrorKind) + "\n\n" + DiagnosticLog.Redact(ex.Message);
        item.Message = item.Error.Split('\n')[0]; log.Write("preview", item.Error);
    }
    public void ReportError(Exception ex) { log.Write("preview", ex.ToString()); Notify(DiagnosticLog.Redact(ex.Message)); }
    public async void Notify(string message)
    {
        if (disposed) return;
        int sequence = ++toastSequence; Toast = message;
        try { await Task.Delay(6500, lifetime.Token); if (sequence == toastSequence) Toast = ""; }
        catch (OperationCanceledException) { }
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(DownloadItemViewModel.State) or nameof(DownloadItemViewModel.Percent) or nameof(DownloadItemViewModel.CanCancel)) RefreshSummary(); }
    private void ItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null) foreach (DownloadItemViewModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
        if (e.NewItems != null) foreach (DownloadItemViewModel item in e.NewItems) item.PropertyChanged += ItemChanged;
        int first = e.NewStartingIndex;
        if (e.OldStartingIndex >= 0) first = first < 0 ? e.OldStartingIndex : Math.Min(first, e.OldStartingIndex);
        int last = e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move ? Math.Max(e.OldStartingIndex, e.NewStartingIndex) : Items.Count - 1;
        for (int i = Math.Max(0, first); i <= last; i++) Items[i].QueueNumber = i + 1;
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
            foreach (string name in new[] { nameof(CompletedCount), nameof(FailedCount), nameof(ActiveCount), nameof(IsEmpty), nameof(Summary), nameof(OverallProgress), nameof(CompletionSummary) }) Raise(name);
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
    public async Task InitializeAsync()
    {
        Task updates = Settings.CheckForAppUpdates ? Updates.CheckAsync() : Task.CompletedTask;
        try { await TrackAsync(Tools.CheckAsync(Settings.ToolFolder)); if (Tools.Missing && !disposed) Panel = "Diagnostics"; }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ReportError(ex); }
        try {
            string recovery = Path.Combine(store.DirectoryPath, "update-queue.json");
            if (File.Exists(recovery) && !lifetime.IsCancellationRequested) {
                await LoadListAsync(recovery);
                File.Move(recovery, recovery + ".restored", true);
                Notify("업데이트 전 목록의 링크와 옵션을 복원했습니다.");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ReportError(ex); }
        await updates;
    }
    private bool CanInstallAppUpdate() => !disposed && !applyingUpdate && !IsRunning && ActiveCount == 0 && !Tools.IsPreparing && !Tools.IsChecking && ItemSettings == null && !Items.Any(i => i.IsRemoving);
    private async Task InstallAppUpdateAsync(string installer)
    {
        if (!CanInstallAppUpdate()) throw new InvalidOperationException("진행 중인 작업을 마친 뒤 업데이트하세요.");
        applyingUpdate = true; Raise(nameof(IsWorkspaceEnabled)); Raise(nameof(CanEditSettings)); CommandManager.InvalidateRequerySuggested();
        try {
            store.Save(Settings); savedSettings = JsonSerializer.Serialize(Settings); Raise(nameof(SettingsStatus));
            await SaveListAsync(Path.Combine(store.DirectoryPath, "update-queue.json"));
            lifetime.Token.ThrowIfCancellationRequested();
            var start = new ProcessStartInfo(installer) { UseShellExecute = true };
            start.ArgumentList.Add("/SP-"); start.ArgumentList.Add("/NORESTART");
            start.ArgumentList.Add("/DIR=" + Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
            using var process = Process.Start(start) ?? throw new InvalidOperationException("설치 프로그램을 열지 못했습니다.");
            Application.Current.MainWindow?.Close();
        }
        finally { applyingUpdate = false; Raise(nameof(IsWorkspaceEnabled)); Raise(nameof(CanEditSettings)); CommandManager.InvalidateRequerySuggested(); }
    }
    private void ToolsChanged(object? sender, PropertyChangedEventArgs e)
    {
        CommandManager.InvalidateRequerySuggested();
        if (e.PropertyName != nameof(ToolWorkspaceViewModel.IsPreparing)) return;
        Raise(nameof(CanEditSettings)); Raise(nameof(IsWorkspaceEnabled)); CommandManager.InvalidateRequerySuggested();
    }
    private async Task InstallToolsAsync()
    {
        string? folder = await Tools.PrepareAsync();
        if (folder == null) return;
        var snapshot = JsonSerializer.Deserialize<PreviewSettings>(JsonSerializer.Serialize(Settings))!;
        snapshot.ToolFolder = folder;
        store.Save(snapshot);
        Settings.ToolFolder = folder;
        savedSettings = JsonSerializer.Serialize(Settings); Raise(nameof(SettingsStatus));
        await Tools.CheckAsync(folder);
        Notify("검증한 도구 묶음이 준비되었습니다. 링크를 추가하세요.");
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
        await Updates.StopAsync();
        await CloseItemSettingsAsync();
        await Task.WhenAll(operations.ToArray());
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; lifetime.Cancel();
        Settings.PropertyChanged -= SettingsChanged;
        Tools.PropertyChanged -= ToolsChanged;
        Items.CollectionChanged -= ItemsChanged;
        foreach (var item in Items) item.PropertyChanged -= ItemChanged;
        lifetime.Dispose(); metadataSlots.Dispose();
    }
}
