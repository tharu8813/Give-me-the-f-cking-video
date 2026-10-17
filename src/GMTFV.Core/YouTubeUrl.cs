using System;
using System.Text.RegularExpressions;

namespace GMTFV.Core {
    public static class YouTubeUrl {
        public static string Normalize(string value) {
            value = value?.Trim();
            if (value != null && Regex.IsMatch(value, @"\A(?:(?:www|m|music)\.)?youtube\.com/|\Ayoutu\.be/", RegexOptions.IgnoreCase)) value = "https://" + value;
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http") || !string.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort)
                throw new ArgumentException("유효한 유튜브 URL이 아닙니다.", nameof(value));
            string host = uri.IdnHost.ToLowerInvariant();
            string id = null;
            string[] parts = uri.AbsolutePath.Trim('/').Split('/');
            if (host == "youtu.be" && parts.Length == 1) id = parts[0];
            else if (host == "youtube.com" || host == "www.youtube.com" || host == "m.youtube.com" || host == "music.youtube.com") {
                if (parts.Length == 2 && (parts[0] == "shorts" || parts[0] == "embed" || parts[0] == "live")) id = parts[1];
                else if (uri.AbsolutePath == "/watch") {
                    foreach (string pair in uri.Query.TrimStart('?').Split('&')) {
                        string[] item = pair.Split(new[] { '=' }, 2);
                        if (item.Length == 2 && item[0] == "v") { id = Uri.UnescapeDataString(item[1]); break; }
                    }
                }
            }
            if (id == null || !Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{11}\z"))
                throw new ArgumentException("유효한 유튜브 영상 URL이 아닙니다.", nameof(value));
            return "https://www.youtube.com/watch?v=" + id;
        }
    }
}
