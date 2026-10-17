using GMTFV.models;
using GMTFV.tools;
using System.Linq;

namespace GMTFV.services {
    /// <summary>목록 저장 형식으로 변환합니다.</summary>
    internal static class PlaylistDataMapper {
        public static ExportVideoData CreateExportVideoData(VideoInfo video) {
            VideoQuality selectedQuality = video.VideoQualities?.FirstOrDefault(q => q.IsSelected);
            return new ExportVideoData {
                Url = string.IsNullOrEmpty(video.ID) ? video.Title : "https://www.youtube.com/watch?v=" + video.ID,
                IsTypeVideo = video.TypeSave?.IsTypeVideo ?? true,
                SubType = video.TypeSave?.SubType ?? "mp4",
                Quality = selectedQuality?.Quality ?? string.Empty,
                Fps = selectedQuality?.Fps ?? 0
            };
        }

    }
}
