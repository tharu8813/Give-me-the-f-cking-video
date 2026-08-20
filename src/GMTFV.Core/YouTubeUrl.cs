using System;
using System.Text.RegularExpressions;

namespace GMTFV.Core {
    /// <summary>
    /// UI 및 외부 도구와 독립적인 YouTube 단일 영상 URL의 검증·정규화 규칙입니다.
    /// </summary>
    public static class YouTubeUrl {
        private static readonly Regex VideoIdPattern = new Regex(
            @"(?:youtube\.com/(?:.*[?&]v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryNormalize(string value, out string normalizedUrl) {
            normalizedUrl = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            Match match = VideoIdPattern.Match(value.Trim());
            if (!match.Success) return false;

            normalizedUrl = "https://www.youtube.com/watch?v=" + match.Groups[1].Value;
            return true;
        }

        public static string Normalize(string value) {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("URL이 비어있습니다.", nameof(value));
            if (!TryNormalize(value, out string normalizedUrl))
                throw new ArgumentException("유효한 유튜브 URL이 아닙니다.", nameof(value));
            return normalizedUrl;
        }
    }
}
