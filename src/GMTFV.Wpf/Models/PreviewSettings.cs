using System.IO;
using GMTFV.Core;
using GMTFV.Wpf.ViewModels;
namespace GMTFV.Wpf.Models;

public sealed class PreviewSettings : ObservableObject
{
    private string outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GMTFV");
    public string OutputFolder { get => outputFolder; set => Set(ref outputFolder, value); }
    private string toolFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GMTFV");
    public string ToolFolder { get => toolFolder; set => Set(ref toolFolder, value); }
    private bool isVideo = true;
    public bool IsVideo { get => isVideo; set => Set(ref isVideo, value); }
    private string format = "mp4";
    public string Format { get => format; set => Set(ref format, value); }
    private int height = 1080, concurrency = 2, audioBitrate = 192;
    public int Height { get => height; set => Set(ref height, value); }
    public int Concurrency { get => concurrency; set => Set(ref concurrency, value); }
    public int AudioBitrate { get => audioBitrate; set => Set(ref audioBitrate, value); }
    private bool allowQualityFallback = true, reduceMotion;
    public bool AllowQualityFallback { get => allowQualityFallback; set => Set(ref allowQualityFallback, value); }
    public bool ReduceMotion { get => reduceMotion; set => Set(ref reduceMotion, value); }
    private bool checkForAppUpdates = true;
    public bool CheckForAppUpdates { get => checkForAppUpdates; set => Set(ref checkForAppUpdates, value); }
    private string fileNameTemplate = "%title%_%id%", videoEncoder = "libx264";
    public string FileNameTemplate { get => fileNameTemplate; set => Set(ref fileNameTemplate, value); }
    public string VideoEncoder { get => videoEncoder; set => Set(ref videoEncoder, value); }
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
