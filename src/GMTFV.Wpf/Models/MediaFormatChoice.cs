using GMTFV.Core;
namespace GMTFV.Wpf.Models;

public sealed record MediaFormatChoice(string Id, string Name, string Description)
{
    public static readonly MediaFormatChoice[] Audio = [
        new("original", "원본 오디오 (변환 없음)", "최적의 오디오 전용 스트림을 그대로 저장합니다. 실제 확장자와 원본 음질을 유지하며 비트레이트는 적용하지 않습니다."),
        new("mp3", "MP3 · 변환이 필요할 수 있음", "실제 원본 코덱이 MP3이면 보존하고, 다른 코덱은 MP3로 변환합니다."),
        new("m4a", "M4A · 호환 원본 변환 생략", "AAC 원본은 그대로 저장하거나 재인코딩 없이 M4A 컨테이너로 바꿉니다."),
        new("aac", "AAC · ADTS 컨테이너", "AAC 원본은 재인코딩 없이 ADTS로 저장합니다. 다른 코덱은 AAC로 변환합니다."),
        new("opus", "Opus · 호환 원본 재인코딩 생략", "Opus 원본은 보존합니다. WebM의 Opus는 재인코딩 없이 Ogg Opus(.opus)로 바꿉니다."),
        new("ogg", "OGG (Vorbis) · 원본에 따라 변환", "Vorbis 원본은 보존하고, Opus를 포함한 다른 코덱은 Vorbis로 변환합니다."),
        new("wav", "WAV · 원본에 따라 변환", "PCM 원본은 가능한 보존합니다. 손실 압축 원본을 WAV로 바꿔도 잃어버린 음질은 복구되지 않습니다."),
        new("flac", "FLAC · 원본에 따라 변환", "FLAC 원본은 보존합니다. 손실 압축 원본을 FLAC로 바꿔도 잃어버린 음질은 복구되지 않습니다.")
    ];
    public static MediaFormatChoice[] For(bool video) => video ? MediaFormats.Video.Select(id => new MediaFormatChoice(id, id.ToUpperInvariant(), "영상 저장 형식")).ToArray() : Audio;
    public static string DescriptionFor(string id) => Audio.FirstOrDefault(f => f.Id == id)?.Description ?? "영상의 실제 제공 형식에 따라 병합·변환합니다.";
    public static string BitrateHint => "비트레이트는 손실 오디오를 재인코딩할 때만 적용됩니다. 원본 저장·컨테이너 변경에는 적용하지 않으며, 높은 값으로 원본 음질을 복구할 수 없습니다.";
}
