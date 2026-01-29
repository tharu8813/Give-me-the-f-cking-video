using System;
using System.Windows.Forms;

namespace GMTFV.tools {
    internal static class Tol {
        // ======================
        // 메시지 박스 유틸
        // ======================

        public static void ShowInfo(string text) {
            MessageBox.Show(text, "정보",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public static void ShowError(string text) {
            MessageBox.Show(text, "오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        public static void ShowWarning(string text) {
            MessageBox.Show(text, "경고",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public static bool ShowQ(string text) {
            return MessageBox.Show(text, "질문",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        // ======================
        // URL 검사
        // ======================

        public static bool IsYouTubeUrl(string url) {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri))
                return false;

            if (!uri.IsAbsoluteUri &&
                !Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
                return false;

            string host = uri.Host.ToLower();

            bool isYoutubeDomain =
                host.Contains("youtube.com") ||
                host.Contains("youtu.be");

            bool hasValidPath =
                uri.AbsolutePath.StartsWith("/watch") ||
                uri.AbsolutePath.StartsWith("/shorts") ||
                host.Contains("youtu.be");

            return isYoutubeDomain && hasValidPath;
        }

        // ======================
        // 컨트롤 활성/비활성
        // ======================

        public static void DisableAllControls(Control parent, bool isEnabled) {
            foreach (Control control in parent.Controls) {
                control.Enabled = isEnabled;

                if (control.HasChildren)
                    DisableAllControls(control, isEnabled);
            }
        }

        // ======================
        // 포맷 목록
        // ======================

        public static readonly string[] VideoFormats =
        {
            "mp4", "mkv", "avi", "mov", "webm", "flv"
        };

        public static readonly string[] AudioFormats =
        {
            "mp3", "m4a", "aac", "opus", "ogg", "wav", "flac"
        };
    }
}
