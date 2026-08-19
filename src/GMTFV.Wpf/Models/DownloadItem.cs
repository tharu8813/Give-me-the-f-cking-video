namespace GMTFV.Wpf.Models;

public sealed class DownloadItem
{
    public string Title { get; init; } = "영상 정보를 불러오는 중입니다";
    public string Channel { get; init; } = "YouTube";
    public string Duration { get; init; } = "–";
    public string Format { get; init; } = "MP4 · 최고 화질";
    public string Status { get; set; } = "대기 중";
    public int Progress { get; set; }
}
