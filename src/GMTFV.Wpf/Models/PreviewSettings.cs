using System.IO;
using GMTFV.Core;
namespace GMTFV.Wpf.Models;

public sealed class PreviewSettings
{
    public string OutputFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GMTFV");
    public string ToolFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GMTFV");
    public bool IsVideo { get; set; } = true;
    public string Format { get; set; } = "mp4";
    public int Height { get; set; } = 1080;
    public int Concurrency { get; set; } = 2;
    public int AudioBitrate { get; set; } = 192;
    public bool AllowQualityFallback { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public string FileNameTemplate { get; set; } = "%title%_%id%";
    public string VideoEncoder { get; set; } = "libx264";
    public void Validate()
    {
        if (!Path.IsPathFullyQualified(OutputFolder) || !Path.IsPathFullyQualified(ToolFolder)) throw new ArgumentException("저장 폴더와 도구 폴더는 전체 경로로 입력하세요.");
        if (!(IsVideo ? MediaFormats.Video : MediaFormats.Audio).Contains(Format)) throw new ArgumentException("선택한 형식이 영상/오디오 종류와 일치하지 않습니다.");
        if (!new[] { 0, 2160, 1440, 1080, 720, 480, 360 }.Contains(Height)) throw new ArgumentException("화질 설정을 확인하세요.");
        if (Concurrency is < 1 or > 16 || AudioBitrate is < 32 or > 512) throw new ArgumentException("동시 다운로드는 1~16개, 비트레이트는 32~512kbps 범위로 설정하세요.");
        if (!new[] { "libx264", "h264_nvenc", "h264_amf", "h264_qsv" }.Contains(VideoEncoder)) throw new ArgumentException("지원하지 않는 인코더입니다.");
        if (string.IsNullOrWhiteSpace(FileNameTemplate)) throw new ArgumentException("파일명 템플릿을 입력하세요.");
    }
}
