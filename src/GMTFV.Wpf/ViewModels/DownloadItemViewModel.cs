using GMTFV.Core;
using GMTFV.Wpf.Services;

namespace GMTFV.Wpf.ViewModels;

/// <summary>목록 한 항목의 메타데이터, 출력 프로필 및 다운로드 진행 상태입니다.</summary>
public sealed class DownloadItemViewModel : ViewModelBase {
    private string sourceUrl;
    private string videoId = string.Empty;
    private string thumbnailUrl = string.Empty;
    private IReadOnlyList<VideoQualityOption> availableQualities = Array.Empty<VideoQualityOption>();
    private string title;
    private string channelName = string.Empty;
    private TimeSpan duration;
    private string durationText = "--:--";
    private DownloadProfile outputProfile;
    private DownloadPhase phase = DownloadPhase.Queued;
    private int progressPercent;
    private string statusText;
    private bool isMetadataLoading;
    private bool hasMetadataError;
    private int queueNumber;
    private string downloadSpeed = string.Empty;

    public DownloadItemViewModel(VideoMetadataResult metadata, DownloadProfile? outputProfile = null) {
        ArgumentNullException.ThrowIfNull(metadata);
        sourceUrl = metadata.SourceUrl;
        title = metadata.Title;
        statusText = "대기 중";
        this.outputProfile = CloneProfile(outputProfile ?? CreateDefaultProfile());
        ApplyMetadata(metadata);
    }

    private DownloadItemViewModel(string normalizedUrl, DownloadProfile outputProfile) {
        sourceUrl = normalizedUrl;
        title = "영상 정보를 불러오는 중…";
        statusText = "영상 정보 불러오는 중…";
        this.outputProfile = CloneProfile(outputProfile);
        isMetadataLoading = true;
    }

    public static DownloadItemViewModel CreateLoading(string normalizedUrl, DownloadProfile outputProfile) => new(normalizedUrl, outputProfile);

    public string SourceUrl { get => sourceUrl; private set { sourceUrl = value; OnPropertyChanged(); } }
    public string VideoId { get => videoId; private set { videoId = value; OnPropertyChanged(); } }
    public string ThumbnailUrl { get => thumbnailUrl; private set { thumbnailUrl = value; OnPropertyChanged(); } }
    public IReadOnlyList<VideoQualityOption> AvailableQualities { get => availableQualities; private set { availableQualities = value; OnPropertyChanged(); } }
    public TimeSpan Duration { get => duration; private set { duration = value; OnPropertyChanged(); } }

    public DownloadProfile OutputProfile {
        get => outputProfile;
        private set {
            outputProfile = CloneProfile(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProfileSummary));
            OnPropertyChanged(nameof(OutputTypeText));
            OnPropertyChanged(nameof(QualityText));
        }
    }

    public string Title { get => title; set { title = value; OnPropertyChanged(); } }
    public string ChannelName { get => channelName; set { channelName = value; OnPropertyChanged(); } }
    public string DurationText { get => durationText; set { durationText = value; OnPropertyChanged(); } }
    public bool IsMetadataLoading { get => isMetadataLoading; private set { isMetadataLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanDownload)); } }
    public bool HasMetadataError { get => hasMetadataError; private set { hasMetadataError = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanDownload)); OnPropertyChanged(nameof(IsFailedPhase)); } }
    public bool CanDownload => !IsMetadataLoading && !HasMetadataError && !string.IsNullOrWhiteSpace(VideoId);
    public int QueueNumber { get => queueNumber; set { queueNumber = Math.Max(0, value); OnPropertyChanged(); OnPropertyChanged(nameof(QueueNumberText)); } }
    public string QueueNumberText => QueueNumber > 0 ? QueueNumber.ToString() : "–";
    public string DownloadSpeed { get => downloadSpeed; private set { downloadSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadSpeedText)); } }
    public string DownloadSpeedText => string.IsNullOrWhiteSpace(DownloadSpeed) ? string.Empty : $"↓ {DownloadSpeed}";

    public DownloadPhase Phase {
        get => phase;
        set {
            phase = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(IsDownloadingPhase));
            OnPropertyChanged(nameof(IsMergingPhase));
            OnPropertyChanged(nameof(IsCompletedPhase));
            OnPropertyChanged(nameof(IsFailedPhase));
        }
    }
    public int ProgressPercent { get => progressPercent; set { progressPercent = Math.Clamp(value, 0, 100); OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); } }
    public string StatusText { get => statusText; set { statusText = value; OnPropertyChanged(); } }

    public string ProgressText => IsMetadataLoading ? "정보 확인 중" : Phase == DownloadPhase.Queued && ProgressPercent == 0 ? "대기" : $"{ProgressPercent}%";
    public string OutputTypeText => OutputProfile.IsVideo ? "비디오" : "오디오";
    public string QualityText => !OutputProfile.IsVideo
        ? OutputProfile.Container.ToUpperInvariant()
        : string.IsNullOrWhiteSpace(OutputProfile.Quality)
            ? "최고 화질"
            : OutputProfile.FramesPerSecond > 0 ? $"{OutputProfile.Quality} · {OutputProfile.FramesPerSecond}fps" : OutputProfile.Quality;
    public string ProfileSummary => OutputProfile.IsVideo
        ? $"비디오 · {QualityText} · {OutputProfile.Container.ToUpperInvariant()}"
        : $"오디오 · {OutputProfile.Container.ToUpperInvariant()}";
    public bool IsDownloadingPhase => Phase is DownloadPhase.Video or DownloadPhase.Audio;
    public bool IsMergingPhase => Phase is DownloadPhase.Merging or DownloadPhase.Processing or DownloadPhase.Finishing;
    public bool IsCompletedPhase => Phase == DownloadPhase.Completed;
    public bool IsFailedPhase => Phase == DownloadPhase.Failed || HasMetadataError;

    public void ApplyMetadata(VideoMetadataResult metadata) {
        SourceUrl = metadata.SourceUrl;
        VideoId = metadata.VideoId;
        ThumbnailUrl = metadata.ThumbnailUrl;
        AvailableQualities = metadata.AvailableQualities;
        Title = metadata.Title;
        ChannelName = metadata.ChannelName;
        Duration = metadata.Duration;
        DurationText = Duration.TotalHours >= 1 ? Duration.ToString(@"h\:mm\:ss") : Duration.ToString(@"m\:ss");
        HasMetadataError = false;
        IsMetadataLoading = false;
        StatusText = "대기 중";
        DownloadSpeed = string.Empty;
        OnPropertyChanged(nameof(ProgressText));
    }

    public void ApplyMetadataError(string message) {
        IsMetadataLoading = false;
        HasMetadataError = true;
        Title = "영상 정보를 불러오지 못했습니다";
        StatusText = message;
        DownloadSpeed = string.Empty;
        OnPropertyChanged(nameof(ProgressText));
    }

    public void ApplyOutputProfile(DownloadProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);
        OutputProfile = profile;
    }

    public void ApplyProgress(DownloadProgress progress) {
        Phase = progress.Phase;
        ProgressPercent = progress.Percent ?? 0;
        StatusText = progress.Message;
        DownloadSpeed = progress.Speed;
    }

    private static DownloadProfile CloneProfile(DownloadProfile profile) => new() {
        IsVideo = profile.IsVideo,
        Container = string.IsNullOrWhiteSpace(profile.Container) ? (profile.IsVideo ? "mp4" : "mp3") : profile.Container,
        Quality = profile.Quality ?? string.Empty,
        FramesPerSecond = profile.FramesPerSecond
    };

    private static DownloadProfile CreateDefaultProfile() => new() { IsVideo = true, Container = "mp4", Quality = string.Empty, FramesPerSecond = 0 };
}
