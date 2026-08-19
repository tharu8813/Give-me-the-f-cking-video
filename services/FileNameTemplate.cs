using System;
using GMTFV.tools;

namespace GMTFV.services {
    /// <summary>
    /// 사용자 파일명 템플릿을 실제 다운로드 파일명으로 변환합니다.
    /// UI와 분리해 테스트하거나 다른 저장 방식에서 재사용할 수 있습니다.
    /// </summary>
    internal static class FileNameTemplate {
        public static string Build(VideoInfo video, string template, int listNumber) {
            if (video == null)
                throw new ArgumentNullException(nameof(video));

            if (string.IsNullOrWhiteSpace(template))
                template = "%title%_%date%";

            string extension = video.TypeSave?.SubType ?? "mp4";
            string name = template
                .Replace("%num3%", listNumber.ToString("D3"))
                .Replace("%num2%", listNumber.ToString("D2"))
                .Replace("%num%", listNumber.ToString())
                .Replace("%no%", listNumber.ToString())
                .Replace("%index%", listNumber.ToString())
                .Replace("%title%", Tol.SanitizeFileName(video.Title ?? "untitled"))
                .Replace("%author%", Tol.SanitizeFileName(video.Author ?? "unknown"))
                .Replace("%date%", DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("%id%", video.ID ?? string.Empty)
                .Replace("%ext%", extension);

            name = Tol.SanitizeFileName(name);
            return (string.IsNullOrWhiteSpace(name) ? "video" : name) + "." + extension;
        }
    }
}
