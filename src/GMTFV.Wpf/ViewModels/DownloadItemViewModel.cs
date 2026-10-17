using GMTFV.Core;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Models;
namespace GMTFV.Wpf.ViewModels;

public sealed class DownloadItemViewModel(string url) : ObservableObject
{
    public string Url { get; } = url;
    private int queueNumber;
    public int QueueNumber { get => queueNumber; internal set => Set(ref queueNumber, value); }
    private string title = "영상 정보를 확인하는 중";
    public string Title { get => title; set => Set(ref title, value); }
    private string channel = "YOUTUBE";
    public string Channel { get => channel; set => Set(ref channel, value); }
    private string duration = "—";
    public string Duration { get => duration; set => Set(ref duration, value); }
    private string? thumbnail;
    public string? Thumbnail { get => thumbnail; set => Set(ref thumbnail, value); }
    private DownloadState state = DownloadState.Queued;
    public DownloadState State
    {
        get => state;
        set { if (Set(ref state, value)) { Raise(nameof(StateText)); Raise(nameof(IsMetadata)); Raise(nameof(CanRetry)); Raise(nameof(CanCancel)); Raise(nameof(HasError)); Raise(nameof(IsIndeterminate)); Raise(nameof(HasOutput)); Raise(nameof(Phase)); Raise(nameof(ProgressText)); } }
    }
    private int? percent;
    public int? Percent { get => percent; set { if (Set(ref percent, value)) { Raise(nameof(Progress)); Raise(nameof(IsIndeterminate)); Raise(nameof(ProgressText)); } } }
    public double Progress => Percent ?? 0;
    public bool IsIndeterminate => Percent == null && State is DownloadState.Metadata or DownloadState.Downloading or DownloadState.Merging or DownloadState.Converting or DownloadState.Retrying or DownloadState.Validating;
    public string ProgressText => State is DownloadState.Downloading && Percent is int p ? $"{p}%" : State == DownloadState.Completed ? "저장됨" : "";
    public int Phase => State switch { DownloadState.Metadata => 0, DownloadState.Queued or DownloadState.Ready => 1, DownloadState.Downloading or DownloadState.Retrying => 2, DownloadState.Merging or DownloadState.Converting or DownloadState.Validating => 3, DownloadState.Completed => 4, _ => -1 };
    private string message = "메타데이터 대기 중";
    public string Message { get => message; set => Set(ref message, value); }
    private string error = "";
    public string Error { get => error; set { if (Set(ref error, value)) Raise(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(Error);
    private DownloadError errorKind;
    public DownloadError ErrorKind { get => errorKind; set { if (Set(ref errorKind, value)) { Raise(nameof(ErrorTitle)); Raise(nameof(ErrorAction)); } } }
    public string ErrorTitle => ErrorKind switch {
        DownloadError.Network or DownloadError.Timeout => "연결을 확인해 주세요",
        DownloadError.MissingTool or DownloadError.Client or DownloadError.PoToken or DownloadError.FFmpeg => "다운로드 도구를 확인해 주세요",
        DownloadError.Permission or DownloadError.DiskFull => "저장 위치를 확인해 주세요",
        DownloadError.Unavailable => "이 영상에 접근할 수 없습니다",
        DownloadError.Format => "다른 화질이나 형식을 선택해 주세요",
        DownloadError.RateLimited => "잠시 기다린 후 다시 시도해 주세요",
        _ => "작업을 마치지 못했습니다"
    };
    public string ErrorAction => RecoveryPolicy.Message(ErrorKind);
    public bool IsMetadata => State == DownloadState.Metadata;
    public bool CanRetry => State is DownloadState.Failed or DownloadState.Cancelled;
    public bool CanCancel => Cancellation != null && !Cancellation.IsCancellationRequested;
    public string StateText => State switch
    {
        DownloadState.Metadata => "정보 조회", DownloadState.Queued => "대기 중", DownloadState.Ready => "준비 완료",
        DownloadState.Downloading => "다운로드", DownloadState.Merging => "병합 중", DownloadState.Converting => "변환 중",
        DownloadState.Retrying => "자동 복구", DownloadState.Validating => "파일 검증", DownloadState.Completed => "완료",
        DownloadState.Cancelled => "취소됨", _ => "실패"
    };
    public VideoMetadata? Metadata { get; set; }
    public ItemDownloadOptions? Options { get; set; }
    public CancellationTokenSource? Cancellation { get; set; }
    private string selection = "";
    public string Selection { get => selection; set => Set(ref selection, value); }
    private string? outputPath;
    public string? OutputPath { get => outputPath; set { if (Set(ref outputPath, value)) Raise(nameof(HasOutput)); } }
    public bool HasOutput => State == DownloadState.Completed && !string.IsNullOrEmpty(OutputPath);
    private bool isRemoving;
    public bool IsRemoving { get => isRemoving; set => Set(ref isRemoving, value); }
}
