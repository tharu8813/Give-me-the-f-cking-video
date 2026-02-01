using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GMTFV.tools {
    internal static class Tol {

        /// <summary>
        /// C:\Users\user\AppData\Roaming\[app_name]을 변환합니다. 만약 디렉토리에 해당하는 폴더가 없을 경우 새로 만듭니다.
        /// </summary>
        public static string AppdataPath {
            get {
                try {
                    string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        Application.ProductName ?? "GMTFV");

                    if (!Directory.Exists(path)) {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                } catch (Exception ex) {
                    Console.WriteLine($"AppdataPath 생성 오류: {ex.Message}");
                    // 폴백: 임시 폴더 사용
                    return Path.GetTempPath();
                }
            }
        }

        // ======================
        // 메시지 박스 유틸
        // ======================

        public static void ShowInfo(string text) {
            try {
                MessageBox.Show(text ?? "정보", "정보",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            } catch (Exception ex) {
                Console.WriteLine($"ShowInfo 오류: {ex.Message}");
            }
        }

        public static void ShowError(string text) {
            try {
                MessageBox.Show(text ?? "오류가 발생했습니다.", "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            } catch (Exception ex) {
                Console.WriteLine($"ShowError 오류: {ex.Message}");
            }
        }

        public static void ShowWarning(string text) {
            try {
                MessageBox.Show(text ?? "경고", "경고",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            } catch (Exception ex) {
                Console.WriteLine($"ShowWarning 오류: {ex.Message}");
            }
        }

        public static bool ShowQ(string text) {
            try {
                return MessageBox.Show(text ?? "계속하시겠습니까?", "질문",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            } catch (Exception ex) {
                Console.WriteLine($"ShowQ 오류: {ex.Message}");
                return false;
            }
        }

        // ======================
        // URL 검사
        // ======================

        public static bool IsYouTubeUrl(string url) {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try {
                if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri))
                    return false;

                if (!uri.IsAbsoluteUri &&
                    !Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
                    return false;

                string host = uri.Host.ToLower();

                bool isYoutubeDomain =
                    host.Contains("youtube.com") ||
                    host.Contains("youtu.be") ||
                    host.Contains("www.youtube.com");

                bool hasValidPath =
                    uri.AbsolutePath.StartsWith("/watch") ||
                    uri.AbsolutePath.StartsWith("/shorts") ||
                    uri.AbsolutePath.StartsWith("/embed") ||
                    host.Contains("youtu.be");

                return isYoutubeDomain && hasValidPath;
            } catch (Exception ex) {
                Console.WriteLine($"IsYouTubeUrl 오류: {ex.Message}");
                return false;
            }
        }

        // ======================
        // 컨트롤 활성/비활성
        // ======================

        public static void DisableAllControls(Control parent, bool isEnabled) {
            if (parent == null) return;

            try {
                foreach (Control control in parent.Controls) {
                    try {
                        control.Enabled = isEnabled;

                        if (control.HasChildren)
                            DisableAllControls(control, isEnabled);
                    } catch (Exception ex) {
                        Console.WriteLine($"컨트롤 비활성화 오류: {ex.Message}");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"DisableAllControls 오류: {ex.Message}");
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

        private const string DefaultDownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

        public class FFmpegProgress {
            public int Percentage { get; set; }
            public string Message { get; set; }
        }

        public static async Task EnsureFFmpegAsync(
            string targetDirectory,
            IProgress<FFmpegProgress> progress = null,
            string downloadUrl = DefaultDownloadUrl) {

            if (string.IsNullOrEmpty(targetDirectory)) {
                throw new ArgumentNullException(nameof(targetDirectory));
            }

            string ffmpegPath = Path.Combine(targetDirectory, "ffmpeg.exe");

            if (File.Exists(ffmpegPath)) {
                progress?.Report(new FFmpegProgress {
                    Percentage = 100,
                    Message = "FFmpeg가 이미 설치되어 있습니다."
                });
                return;
            }

            if (!ShowQ("FFmpeg 구성 요소가 설치되어 있지 않습니다.\n" +
                "해당 기능을 사용하려면 FFmpeg 다운로드가 필요합니다.\n\n" +
                "지금 다운로드하시겠습니까?")) {
                Application.Exit();
                return;
            }

            string zipPath = Path.Combine(Path.GetTempPath(), "ffmpeg.zip");
            string extractPath = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");

            try {
                if (Directory.Exists(extractPath)) {
                    try {
                        Directory.Delete(extractPath, true);
                    } catch (Exception ex) {
                        Console.WriteLine($"임시 폴더 삭제 실패: {ex.Message}");
                    }
                }

                if (!Directory.Exists(targetDirectory)) {
                    Directory.CreateDirectory(targetDirectory);
                }

                using (var wc = new WebClient()) {
                    wc.DownloadProgressChanged += (s, e) => {
                        try {
                            progress?.Report(new FFmpegProgress {
                                Percentage = e.ProgressPercentage,
                                Message = $"FFmpeg 다운로드 중... {e.ProgressPercentage}%"
                            });
                        } catch { }
                    };

                    await wc.DownloadFileTaskAsync(new Uri(downloadUrl), zipPath);
                }

                progress?.Report(new FFmpegProgress {
                    Percentage = 0,
                    Message = "압축 해제 중..."
                });

                await Task.Run(() => {
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                });

                string extractedFFmpeg = Directory
                    .GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();

                if (extractedFFmpeg == null)
                    throw new FileNotFoundException("ffmpeg.exe를 찾을 수 없습니다.");

                File.Copy(extractedFFmpeg, ffmpegPath, overwrite: true);

                progress?.Report(new FFmpegProgress {
                    Percentage = 100,
                    Message = "FFmpeg 준비 완료"
                });
            } catch (Exception ex) {
                throw new Exception($"FFmpeg 설치 실패: {ex.Message}", ex);
            } finally {
                try {
                    if (File.Exists(zipPath)) {
                        File.Delete(zipPath);
                    }
                    if (Directory.Exists(extractPath)) {
                        Directory.Delete(extractPath, true);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"임시 파일 정리 실패: {ex.Message}");
                }
            }
        }

        public static List<DataGridViewRow> GetCheckedRows(DataGridView grid, string checkColumnName = "Select") {
            List<DataGridViewRow> checkedRows = new List<DataGridViewRow>();

            if (grid == null) return checkedRows;

            try {
                foreach (DataGridViewRow row in grid.Rows) {
                    if (!row.IsNewRow) {
                        try {
                            if (row.Cells[checkColumnName]?.Value != null &&
                                Convert.ToBoolean(row.Cells[checkColumnName].Value)) {
                                checkedRows.Add(row);
                            }
                        } catch (Exception ex) {
                            Console.WriteLine($"행 체크 확인 오류: {ex.Message}");
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"GetCheckedRows 오류: {ex.Message}");
            }

            return checkedRows;
        }

        /// <summary>
        /// 파일명에 사용할 수 없는 문자를 제거하고 안전한 파일명을 반환합니다.
        /// </summary>
        public static string SanitizeFileName(string fileName, string replacement = "_") {
            if (string.IsNullOrWhiteSpace(fileName)) {
                return "unnamed";
            }

            try {
                string sanitized = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));

                // 빈 문자열인 경우
                if (string.IsNullOrWhiteSpace(sanitized)) {
                    return "unnamed";
                }

                // 길이 제한 (Windows 경로 제한 고려)
                if (sanitized.Length > 200) {
                    sanitized = sanitized.Substring(0, 200);
                }

                return sanitized;
            } catch (Exception ex) {
                Console.WriteLine($"SanitizeFileName 오류: {ex.Message}");
                return "unnamed";
            }
        }

        /// <summary>
        /// 디렉토리가 존재하는지 확인하고, 없으면 생성합니다.
        /// </summary>
        public static bool EnsureDirectoryExists(string path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return false;
            }

            try {
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                return true;
            } catch (Exception ex) {
                Console.WriteLine($"디렉토리 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 프로세스를 안전하게 종료합니다.
        /// </summary>
        public static void KillProcess(string processName) {
            if (string.IsNullOrWhiteSpace(processName)) {
                return;
            }

            try {
                foreach (Process process in Process.GetProcessesByName(processName)) {
                    try {
                        if (!process.HasExited) {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"{processName} 프로세스 종료 오류: {ex.Message}");
                    } finally {
                        process?.Dispose();
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"{processName} 프로세스 검색 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일 크기를 사람이 읽기 쉬운 형식으로 변환합니다.
        /// </summary>
        public static string FormatFileSize(long bytes) {
            try {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = bytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1) {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            } catch (Exception ex) {
                Console.WriteLine($"FormatFileSize 오류: {ex.Message}");
                return $"{bytes} B";
            }
        }

        /// <summary>
        /// 네트워크 연결 상태를 확인합니다.
        /// </summary>
        public static async Task<bool> CheckInternetConnectionAsync() {
            try {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) }) {
                    var response = await client.GetAsync("https://www.google.com");
                    return response.IsSuccessStatusCode;
                }
            } catch {
                return false;
            }
        }
    }
}