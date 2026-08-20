using GMTFV.Core;
using GMTFV.models;
using GMTFV.tools;
using System.Linq;

namespace GMTFV.services {
    /// <summary>
    /// 현재 WinForms 모델과 WPF에서도 사용할 Core 목록 계약 사이의 변환을 담당합니다.
    /// 파일 대화상자나 화면 제어를 포함하지 않습니다.
    /// </summary>
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

        public static PlaylistItem ToPlaylistItem(ExportVideoData data) {
            if (data == null) return null;
            return new PlaylistItem {
                Url = data.Url,
                Profile = new DownloadProfile {
                    IsVideo = data.IsTypeVideo,
                    Container = data.SubType,
                    Quality = data.Quality,
                    FramesPerSecond = data.Fps
                }
            };
        }
    }
}
