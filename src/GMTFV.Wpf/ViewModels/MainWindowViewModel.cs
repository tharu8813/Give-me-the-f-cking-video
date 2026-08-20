using GMTFV.Core;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace GMTFV.Wpf.ViewModels;

/// <summary>
/// P3의 최소 앱 셸 상태입니다. 실제 URL·목록·다운로드 바인딩은 P5~P7에서 추가합니다.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase {
    private string statusMessage = BuildStatusMessage();
    private DownloadItemViewModel? selectedDownload;
    private bool isListEmpty = true;
    private string downloadCountText = "다운로드 0개";
    private string urlInput = string.Empty;
    private readonly HashSet<string> loadingUrls = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim metadataGate = new(4, 4);
    private readonly CancellationTokenSource metadataCancellation = new();
    private readonly YtDlpMetadataService metadataService;
    private readonly YtDlpDownloadService downloadService = new();
    private readonly SettingsStore settingsStore = new();
    private DownloadSettings settings;
    private CancellationTokenSource? downloadCancellation;
    private bool isDownloading;
    private int overallProgressPercent;
    private string overallProgressText = "다운로드를 시작할 항목을 추가하세요.";
    private string queueViewMode = "자세히";
    private int selectedItemCount;

    public MainWindowViewModel(YtDlpMetadataService? metadataService = null) {
        this.metadataService = metadataService ?? new YtDlpMetadataService();
        settings = settingsStore.Load();
        ShellActionCommand = new RelayCommand(parameter => {
            StatusMessage = parameter as string ?? "이 기능은 이후 단계에서 연결됩니다.";
        });
        AddUrlCommand = new RelayCommand(_ => _ = AddUrlInBackgroundAsync(), _ => !string.IsNullOrWhiteSpace(UrlInput));
        StartDownloadsCommand = new AsyncRelayCommand(_ => StartDownloadsAsync(), _ => !IsDownloading && DownloadItems.Any(item => item.CanDownload && item.Phase is not DownloadPhase.Completed));
        CancelDownloadsCommand = new RelayCommand(_ => CancelDownloads(), _ => IsDownloading);
        DownloadItems.CollectionChanged += DownloadItems_CollectionChanged;
    }

    public RelayCommand ShellActionCommand { get; }
    public RelayCommand AddUrlCommand { get; }
    public AsyncRelayCommand StartDownloadsCommand { get; }
    public RelayCommand CancelDownloadsCommand { get; }

    public string UrlInput {
        get => urlInput;
        set {
            if (urlInput == value) return;
            urlInput = value;
            OnPropertyChanged();
            AddUrlCommand.RaiseCanExecuteChanged();
        }
    }

    public DownloadSettings Settings => settings;

    public void ApplySettings(DownloadSettings newSettings) {
        settings = newSettings?.Clone() ?? throw new ArgumentNullException(nameof(newSettings));
        settingsStore.Save(settings);
        StatusMessage = "다운로드 설정을 저장했습니다.";
    }

    public async Task ImportPlaylistAsync(string filePath) {
        if (IsDownloading) return;
        IReadOnlyList<PlaylistFileItem> entries = await Task.Run(() => PlaylistFileService.Read(filePath));
        if (entries.Count == 0) { StatusMessage = "불러올 유효한 YouTube URL이 없습니다."; return; }
        StatusMessage = $"{entries.Count}개 영상 정보를 백그라운드에서 불러오는 중…";
        bool[] results = await Task.WhenAll(entries.Select(entry => QueueVideoAdditionAsync(entry.Url ?? string.Empty, entry.ToProfile())));
        int added = results.Count(result => result);
        StatusMessage = $"목록 불러오기 완료 · {added}/{entries.Count}개 추가";
    }

    public void ExportPlaylist(string filePath, bool json) {
        if (json) PlaylistFileService.WriteJson(filePath, DownloadItems, settings);
        else PlaylistFileService.WriteText(filePath, DownloadItems);
        StatusMessage = "다운로드 목록을 내보냈습니다.";
    }

    public async Task ImportChromeTabsAsync(IReadOnlyList<string> urls) {
        if (IsDownloading) return;
        if (urls.Count == 0) return;
        StatusMessage = $"Chrome 탭 {urls.Count}개의 정보를 백그라운드에서 불러오는 중…";
        bool[] results = await Task.WhenAll(urls.Select(url => QueueVideoAdditionAsync(url, settings.ToProfile())));
        int added = results.Count(result => result);
        StatusMessage = added == 0 ? "Chrome에서 받은 탭은 모두 이미 목록에 있거나 추가할 수 없습니다." : $"Chrome 탭 가져오기 완료 · {added}/{urls.Count}개 추가";
    }

    public void ReportChromeConnectionUnavailable() => StatusMessage = "Chrome 탭 가져오기 통신 포트(43128)를 사용할 수 없습니다. 다른 GMTFV 창을 종료한 뒤 다시 실행해주세요.";

    public async Task<int> AddUrlsInBackgroundAsync(IEnumerable<string> urls) {
        if (IsDownloading) return 0;
        string[] candidates = urls.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (candidates.Length == 0) return 0;
        StatusMessage = $"{candidates.Length}개 링크의 영상 정보를 백그라운드에서 불러오는 중…";
        bool[] results = await Task.WhenAll(candidates.Select(url => QueueVideoAdditionAsync(url, settings.ToProfile())));
        int added = results.Count(result => result);
        StatusMessage = $"링크 추가 완료 · {added}/{candidates.Length}개 추가";
        return added;
    }

    /// <summary>
    /// P6부터 URL을 추가하고 P7부터 진행률을 갱신할 가상화 목록 원본입니다.
    /// </summary>
    public ObservableCollection<DownloadItemViewModel> DownloadItems { get; } = new();

    public DownloadItemViewModel? SelectedDownload {
        get => selectedDownload;
        set {
            if (selectedDownload == value) return;
            if (selectedDownload is not null) selectedDownload.PropertyChanged -= SelectedDownload_PropertyChanged;
            selectedDownload = value;
            if (selectedDownload is not null) selectedDownload.PropertyChanged += SelectedDownload_PropertyChanged;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    private void SelectedDownload_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName is nameof(DownloadItemViewModel.Title) or nameof(DownloadItemViewModel.StatusText)) OnPropertyChanged(nameof(SelectionSummary));
    }

    public string QueueViewMode {
        get => queueViewMode;
        set {
            if (value is not ("자세히" or "단순" or "격자") || queueViewMode == value) return;
            queueViewMode = value;
            OnPropertyChanged();
        }
    }

    public int SelectedItemCount {
        get => selectedItemCount;
        private set {
            if (selectedItemCount == value) return;
            selectedItemCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public bool IsListEmpty {
        get => isListEmpty;
        private set {
            if (isListEmpty == value) return;
            isListEmpty = value;
            OnPropertyChanged();
        }
    }

    public string DownloadCountText {
        get => downloadCountText;
        private set {
            if (downloadCountText == value) return;
            downloadCountText = value;
            OnPropertyChanged();
        }
    }

    public bool IsDownloading {
        get => isDownloading;
        private set {
            if (isDownloading == value) return;
            isDownloading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanModifyQueue));
            OnPropertyChanged(nameof(DownloadActionText));
            StartDownloadsCommand.RaiseCanExecuteChanged();
            CancelDownloadsCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanModifyQueue => !IsDownloading;

    public string DownloadActionText => IsDownloading ? "다운로드 진행 중" : "다운로드 시작";

    public int OverallProgressPercent {
        get => overallProgressPercent;
        private set {
            if (overallProgressPercent == value) return;
            overallProgressPercent = value;
            OnPropertyChanged();
        }
    }

    public string OverallProgressText {
        get => overallProgressText;
        private set {
            if (overallProgressText == value) return;
            overallProgressText = value;
            OnPropertyChanged();
        }
    }

    public string SelectionSummary => SelectedItemCount > 1
        ? $"{SelectedItemCount}개 항목을 선택했습니다. 일괄 설정이나 삭제를 적용할 수 있습니다."
        : SelectedDownload is null
        ? "선택한 항목이 없습니다. 목록에서 영상을 선택하면 상태와 진행률을 확인할 수 있습니다."
        : $"선택됨 · {SelectedDownload.Title} · {SelectedDownload.StatusText}";

    public void SetSelectedItems(IReadOnlyCollection<DownloadItemViewModel> items) {
        SelectedItemCount = items.Count;
        if (items.Count == 1) SelectedDownload = items.First();
    }

    public void RemoveItems(IEnumerable<DownloadItemViewModel> items) {
        DownloadItemViewModel[] targets = items.Distinct().Where(DownloadItems.Contains).ToArray();
        foreach (DownloadItemViewModel item in targets) DownloadItems.Remove(item);
        SelectedItemCount = 0;
        StatusMessage = $"목록에서 {targets.Length}개 항목을 삭제했습니다. 이미 저장된 파일은 삭제하지 않았습니다.";
    }

    public void ApplyProfileToItems(IEnumerable<DownloadItemViewModel> items, bool isVideo, string quality, int framesPerSecond) {
        DownloadItemViewModel[] targets = items.Where(item => item.CanDownload).Distinct().ToArray();
        foreach (DownloadItemViewModel item in targets) {
            VideoQualityOption? resolvedQuality = isVideo ? ResolveRequestedQuality(item, quality, framesPerSecond) : null;
            item.ApplyOutputProfile(new DownloadProfile {
                IsVideo = isVideo,
                Container = isVideo ? "mp4" : "mp3",
                Quality = resolvedQuality is null ? string.Empty : $"{resolvedQuality.Height}p",
                FramesPerSecond = resolvedQuality?.FramesPerSecond ?? 0
            });
        }
        StatusMessage = targets.Length == 0 ? "설정을 적용할 준비 완료 항목이 없습니다." : $"{targets.Length}개 항목에 {targets[0].ProfileSummary} 설정을 적용했습니다.";
    }

    public string StatusMessage {
        get => statusMessage;
        private set {
            if (statusMessage == value) return;
            statusMessage = value;
            OnPropertyChanged();
        }
    }

    private static string BuildStatusMessage() {
        return "YouTube 링크를 입력하거나 Chrome 탭에서 영상을 가져오세요.";
    }

    private void DownloadItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        IsListEmpty = DownloadItems.Count == 0;
        DownloadCountText = $"다운로드 {DownloadItems.Count}개";
        if (IsListEmpty) SelectedDownload = null;
        for (int index = 0; index < DownloadItems.Count; index++) DownloadItems[index].QueueNumber = index + 1;
        StartDownloadsCommand.RaiseCanExecuteChanged();
    }

    private async Task AddUrlInBackgroundAsync() {
        string normalizedUrl;
        try {
            normalizedUrl = YouTubeUrl.Normalize(UrlInput);
        } catch (ArgumentException ex) {
            StatusMessage = ex.Message;
            return;
        }

        UrlInput = string.Empty;
        StatusMessage = "영상 정보를 백그라운드에서 불러오는 중입니다. 다른 링크를 계속 추가할 수 있습니다.";
        bool added = await QueueVideoAdditionAsync(normalizedUrl, settings.ToProfile());
        StatusMessage = added ? "영상 정보를 추가했습니다." : "이미 목록에 있거나 영상 정보를 불러오지 못했습니다.";
    }

    private async Task StartDownloadsAsync() {
        DownloadItemViewModel[] targets = DownloadItems.Where(item => item.CanDownload && item.Phase is not DownloadPhase.Completed).ToArray();
        if (targets.Length == 0) return;

        downloadCancellation = new CancellationTokenSource();
        IsDownloading = true;
        OverallProgressPercent = 0;
        OverallProgressText = $"{targets.Length}개 항목을 다운로드 대기열에 넣었습니다.";
        StatusMessage = "다운로드를 준비하는 중입니다…";

        using var queue = new DownloadQueueService(Math.Clamp(settings.MaxConcurrentDownloads, 1, 5));
        try {
            await Task.WhenAll(targets.Select((item, index) => DownloadItemAsync(item, index + 1, targets.Length, queue, downloadCancellation.Token)));
            if (!downloadCancellation.IsCancellationRequested) {
                int completed = targets.Count(item => item.Phase == DownloadPhase.Completed);
                int failed = targets.Count(item => item.Phase == DownloadPhase.Failed);
                OverallProgressText = failed == 0 ? $"다운로드 완료 · 성공 {completed}/{targets.Length}" : $"다운로드 완료 · 성공 {completed}, 실패 {failed}";
                StatusMessage = OverallProgressText;
            }
        } catch (OperationCanceledException) {
            OverallProgressText = "다운로드가 취소되었습니다.";
            StatusMessage = OverallProgressText;
        } finally {
            IsDownloading = false;
            downloadCancellation?.Dispose();
            downloadCancellation = null;
            StartDownloadsCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task DownloadItemAsync(DownloadItemViewModel item, int ordinal, int total, DownloadQueueService queue, CancellationToken cancellationToken) {
        int slot = -1;
        try {
            item.ApplyProgress(new DownloadProgress(DownloadPhase.Queued, 0, $"대기열에서 순서를 기다리는 중… ({ordinal}/{total})"));
            slot = await queue.AcquireSlotAsync(cancellationToken);
            item.ApplyProgress(new DownloadProgress(DownloadPhase.Video, 0, "다운로드 준비 중…"));
            SelectedDownload = item;
            var progress = new Progress<DownloadProgress>(state => {
                item.ApplyProgress(state);
                UpdateOverallProgress(total);
            });
            DownloadProfile profile = ResolveProfileForDownload(item);
            item.ApplyOutputProfile(profile);
            int listNumber = DownloadItems.IndexOf(item) + 1;
            await downloadService.DownloadAsync(new DownloadRequest(item.SourceUrl, item.VideoId, item.Title, item.ChannelName, item.Duration, settings.OutputDirectory, listNumber, profile, settings), progress, cancellationToken);
            UpdateOverallProgress(total);
        } catch (OperationCanceledException) {
            item.ApplyProgress(new DownloadProgress(DownloadPhase.Cancelled, item.ProgressPercent, "사용자가 다운로드를 취소했습니다."));
            throw;
        } catch (Exception ex) {
            item.ApplyProgress(new DownloadProgress(DownloadPhase.Failed, item.ProgressPercent, "실패: " + ex.Message));
            UpdateOverallProgress(total);
        } finally {
            if (slot >= 0) queue.ReleaseSlot(slot);
        }
    }

    private void CancelDownloads() {
        if (!IsDownloading) return;
        StatusMessage = "다운로드를 취소하는 중입니다…";
        downloadCancellation?.Cancel();
        downloadService.CancelActiveDownloads();
    }

    private void UpdateOverallProgress(int total) {
        if (total <= 0) return;
        double sum = DownloadItems.Where(item => item.Phase is not DownloadPhase.Queued).Sum(item => item.ProgressPercent);
        OverallProgressPercent = Math.Clamp((int)Math.Round(sum / total), 0, 100);
        int completed = DownloadItems.Count(item => item.Phase == DownloadPhase.Completed);
        OverallProgressText = $"전체 진행률 {OverallProgressPercent}% · 완료 {completed}/{total}";
    }

    public void Dispose() {
        metadataCancellation.Cancel();
        downloadCancellation?.Cancel();
        downloadService.Dispose();
        downloadCancellation?.Dispose();
        metadataCancellation.Dispose();
    }

    private async Task<bool> QueueVideoAdditionAsync(string url, DownloadProfile outputProfile) {
        if (!YouTubeUrl.TryNormalize(url, out string normalizedUrl)) return false;
        if (DownloadItems.Any(item => string.Equals(item.SourceUrl, normalizedUrl, StringComparison.OrdinalIgnoreCase))) return false;

        lock (loadingUrls) {
            if (!loadingUrls.Add(normalizedUrl)) return false;
        }

        var item = DownloadItemViewModel.CreateLoading(normalizedUrl, outputProfile);
        DownloadItems.Add(item);
        SelectedDownload = item;
        bool gateEntered = false;
        try {
            await metadataGate.WaitAsync(metadataCancellation.Token);
            gateEntered = true;
            VideoMetadataResult metadata = await metadataService.GetAsync(normalizedUrl, metadataCancellation.Token);
            if (!DownloadItems.Contains(item)) return false;
            DownloadItemViewModel? duplicate = DownloadItems.FirstOrDefault(existing => !ReferenceEquals(existing, item) && !string.IsNullOrWhiteSpace(existing.VideoId) && string.Equals(existing.VideoId, metadata.VideoId, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null) {
                DownloadItems.Remove(item);
                SelectedDownload = duplicate;
                return false;
            }
            item.ApplyMetadata(metadata);
            StartDownloadsCommand.RaiseCanExecuteChanged();
            return true;
        } catch (OperationCanceledException) {
            if (DownloadItems.Contains(item)) item.ApplyMetadataError("영상 정보 불러오기가 취소되었습니다.");
            return false;
        } catch (Exception ex) {
            if (DownloadItems.Contains(item)) item.ApplyMetadataError(ex.Message);
            return false;
        } finally {
            if (gateEntered) metadataGate.Release();
            lock (loadingUrls) loadingUrls.Remove(normalizedUrl);
        }
    }

    private static DownloadProfile ResolveProfileForDownload(DownloadItemViewModel item) {
        DownloadProfile profile = item.OutputProfile;
        if (!profile.IsVideo || string.IsNullOrWhiteSpace(profile.Quality)) return profile;
        VideoQualityOption? quality = ResolveRequestedQuality(item, profile.Quality, profile.FramesPerSecond);
        return new DownloadProfile {
            IsVideo = true,
            Container = profile.Container,
            Quality = quality is null ? string.Empty : $"{quality.Height}p",
            FramesPerSecond = quality?.FramesPerSecond ?? 0
        };
    }

    private static VideoQualityOption? ResolveRequestedQuality(DownloadItemViewModel item, string requestedQuality, int requestedFps) {
        if (item.AvailableQualities.Count == 0 || string.IsNullOrWhiteSpace(requestedQuality)) return null;
        bool parsed = int.TryParse(requestedQuality.Trim().TrimEnd('p', 'P'), out int requestedHeight);
        IEnumerable<VideoQualityOption> exact = parsed ? item.AvailableQualities.Where(option => option.Height == requestedHeight) : Enumerable.Empty<VideoQualityOption>();
        VideoQualityOption? match = requestedFps > 0
            ? exact.Where(option => option.FramesPerSecond == requestedFps).FirstOrDefault()
            : exact.OrderByDescending(option => option.FramesPerSecond).FirstOrDefault();
        return match ?? item.AvailableQualities.OrderByDescending(option => option.Height).ThenByDescending(option => option.FramesPerSecond).First();
    }
}
