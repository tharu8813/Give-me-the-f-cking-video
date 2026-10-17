using GMTFV.Core;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Models;
namespace GMTFV.Wpf.ViewModels;

public sealed class DownloadItemViewModel(string url) : ObservableObject
{
    public string Url { get; } = url;
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
        set { if (Set(ref state, value)) { Raise(nameof(StateText)); Raise(nameof(IsMetadata)); Raise(nameof(CanRetry)); Raise(nameof(CanCancel)); Raise(nameof(HasError)); Raise(nameof(IsIndeterminate)); } }
    }
    private int? percent;
    public int? Percent { get => percent; set { if (Set(ref percent, value)) { Raise(nameof(Progress)); Raise(nameof(IsIndeterminate)); Raise(nameof(ProgressText)); } } }
    public double Progress => Percent ?? 0;
    public bool IsIndeterminate => Percent == null && CanCancel;
    public string ProgressText => Percent is int p ? $"{p}%" : "—";
    private string message = "메타데이터 대기 중";
    public string Message { get => message; set => Set(ref message, value); }
    private string error = "";
    public string Error { get => error; set { if (Set(ref error, value)) Raise(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(Error);
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
    public string? OutputPath { get; set; }
    private bool isRemoving;
    public bool IsRemoving { get => isRemoving; set => Set(ref isRemoving, value); }
}
