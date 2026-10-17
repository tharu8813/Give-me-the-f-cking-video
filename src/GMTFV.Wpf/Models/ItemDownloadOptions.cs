using GMTFV.Core;
namespace GMTFV.Wpf.Models;

public sealed class ItemDownloadOptions
{
    public bool IsVideo { get; set; } = true;
    public string Format { get; set; } = "mp4";
    public int? Height { get; set; }
    public int? Fps { get; set; }
    public void Validate()
    {
        if (!(IsVideo ? MediaFormats.Video : MediaFormats.Audio).Contains(Format) || Height <= 0 || Fps <= 0)
            throw new ArgumentException("영상별 형식 또는 화질 설정이 올바르지 않습니다.");
    }
}

public sealed record QualityChoice(int? Height, int? Fps)
{
    public override string ToString() => Height == null ? "최고 화질 (자동)" : $"{Height}p" + (Fps > 0 ? $" · {Fps} FPS" : "");
}
public sealed record SubtitleTrack(string Language, bool Automatic, string Name)
{
    public override string ToString() => Name + (Automatic ? " (자동 생성)" : "");
}
