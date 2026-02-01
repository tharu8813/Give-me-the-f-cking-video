using GMTFV.Properties;
using GMTFV.tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace GMTFV.Start {
    public partial class VideoInfoForm : DevForm {
        private readonly VideoInfo videoInfo;
        private string videoUrl;
        private List<SubtitleInfo> availableSubtitles = new List<SubtitleInfo>();
        private bool isClosing = false;
        private CancellationTokenSource subtitleLoadCts;

        public VideoInfoForm(VideoInfo videoInfo) {
            InitializeComponent();

            if (videoInfo == null) {
                throw new ArgumentNullException(nameof(videoInfo), "VideoInfo는 null일 수 없습니다.");
            }

            this.videoInfo = videoInfo;
            this.videoUrl = $"https://www.youtube.com/watch?v={videoInfo.ID}";
        }

        private async void VideoInfoForm_Load(object sender, EventArgs e) {
            try {
                // 기본 정보 표시
                label1.Text = videoInfo.Title ?? "제목 없음";
                label2.Text = $"👤 저자: {videoInfo.Author ?? "알 수 없음"}";
                label3.Text = $"⏱️ 길이: {videoInfo.VideoLength}";
                label4.Text = $"📅 업로드 일자: {videoInfo.UploadDate:yyyy-MM-dd}";

                if (videoInfo.Image != null) {
                    try {
                        pictureBox1.Image = videoInfo.Image;
                    } catch (Exception ex) {
                        Console.WriteLine($"이미지 로드 실패: {ex.Message}");
                    }
                }

                // URL 표시
                txtUrl.Text = videoUrl;

                // 화질 옵션 설정
                if (videoInfo.VideoQualities != null && videoInfo.VideoQualities.Any()) {
                    try {
                        comboBox1.Items.AddRange(videoInfo.VideoQualities.ToArray());
                        int selectedIndex = comboBox1.Items.IndexOf(videoInfo.VideoQualities.FirstOrDefault(v => v.IsSelected));
                        if (selectedIndex >= 0) {
                            comboBox1.SelectedIndex = selectedIndex;
                        } else if (comboBox1.Items.Count > 0) {
                            comboBox1.SelectedIndex = 0;
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"화질 옵션 설정 오류: {ex.Message}");
                    }
                }

                // 비디오/오디오 옵션 설정
                if (videoInfo.TypeSave != null) {
                    radioButton1.Checked = videoInfo.TypeSave.IsTypeVideo;
                    radioButton2.Checked = !videoInfo.TypeSave.IsTypeVideo;
                } else {
                    radioButton1.Checked = true;
                }

                UpdateCombo();

                // 자막 목록 불러오기
                if (!string.IsNullOrEmpty(videoInfo.ID)) {
                    await UpdateCCAsync();
                } else {
                    comboBox3.Items.Clear();
                    comboBox3.Items.Add("영상 ID가 없어 자막을 불러올 수 없습니다");
                    comboBox3.SelectedIndex = 0;
                    comboBox3.Enabled = false;
                    button2.Enabled = false;
                }
            } catch (Exception ex) {
                MessageBox.Show($"폼 로드 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateCCAsync() {
            subtitleLoadCts?.Cancel();
            subtitleLoadCts = new CancellationTokenSource();
            var cancellationToken = subtitleLoadCts.Token;

            try {
                comboBox3.Items.Clear();
                comboBox3.Items.Add("자막 불러오는 중...");
                comboBox3.SelectedIndex = 0;
                comboBox3.Enabled = false;
                button2.Enabled = false;

                // yt-dlp로 자막 정보 가져오기
                availableSubtitles = await GetSubtitlesWithYtDlp(videoUrl, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                comboBox3.Items.Clear();

                if (availableSubtitles != null && availableSubtitles.Any()) {
                    comboBox3.DisplayMember = "DisplayName";
                    comboBox3.ValueMember = "LanguageCode";
                    comboBox3.Items.AddRange(availableSubtitles.ToArray());

                    // 한국어 자막 우선 선택
                    var koreanSubtitle = availableSubtitles.FirstOrDefault(s => s.LanguageCode.StartsWith("ko"));
                    if (koreanSubtitle != null) {
                        comboBox3.SelectedItem = koreanSubtitle;
                    } else if (comboBox3.Items.Count > 0) {
                        comboBox3.SelectedIndex = 0;
                    }

                    comboBox3.Enabled = true;
                    button2.Enabled = true;
                } else {
                    comboBox3.Items.Add("사용 가능한 자막이 없습니다");
                    comboBox3.SelectedIndex = 0;
                }
            } catch (OperationCanceledException) {
                Console.WriteLine("자막 로드 취소됨");
            } catch (Exception ex) {
                comboBox3.Items.Clear();
                comboBox3.Items.Add("자막을 불러올 수 없습니다");
                comboBox3.SelectedIndex = 0;
                Console.WriteLine($"자막 불러오기 오류: {ex.Message}");
            }
        }

        private async Task<List<SubtitleInfo>> GetSubtitlesWithYtDlp(string url, CancellationToken cancellationToken) {
            List<SubtitleInfo> subtitles = new List<SubtitleInfo>();
            string ytdlpPath = Path.Combine(Tol.AppdataPath, "yt-dlp.exe");

            if (!File.Exists(ytdlpPath)) {
                throw new Exception("yt-dlp.exe를 찾을 수 없습니다.");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = ytdlpPath,
                Arguments = $"--dump-json --skip-download \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Process process = null;
            try {
                process = new Process { StartInfo = startInfo };
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => {
                    while (!process.WaitForExit(1000)) {
                        if (cancellationToken.IsCancellationRequested) {
                            try {
                                process.Kill();
                            } catch { }
                            throw new OperationCanceledException();
                        }
                    }
                }, cancellationToken);

                if (process.ExitCode != 0) {
                    throw new Exception($"yt-dlp 오류: {error.ToString()}");
                }

                string jsonOutput = output.ToString();
                if (string.IsNullOrWhiteSpace(jsonOutput)) {
                    return subtitles;
                }

                // JSON 파싱하여 자막 정보 추출
                try {
                    JObject data = JObject.Parse(jsonOutput);
                    JObject subtitlesObj = data["subtitles"] as JObject;
                    JObject autoSubtitlesObj = data["automatic_captions"] as JObject;

                    // 일반 자막
                    if (subtitlesObj != null) {
                        foreach (var subtitle in subtitlesObj) {
                            try {
                                string langCode = subtitle.Key;
                                JArray formats = subtitle.Value as JArray;

                                if (formats != null && formats.Count > 0) {
                                    string langName = GetLanguageName(langCode);
                                    subtitles.Add(new SubtitleInfo {
                                        LanguageCode = langCode,
                                        LanguageName = langName,
                                        IsAutoGenerated = false
                                    });
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"자막 항목 파싱 오류: {ex.Message}");
                            }
                        }
                    }

                    // 자동 생성 자막 (일반 자막이 없는 경우에만)
                    if (autoSubtitlesObj != null && subtitles.Count == 0) {
                        foreach (var subtitle in autoSubtitlesObj) {
                            try {
                                string langCode = subtitle.Key;
                                JArray formats = subtitle.Value as JArray;

                                if (formats != null && formats.Count > 0) {
                                    string langName = GetLanguageName(langCode);
                                    subtitles.Add(new SubtitleInfo {
                                        LanguageCode = langCode,
                                        LanguageName = langName,
                                        IsAutoGenerated = true
                                    });
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"자동 자막 항목 파싱 오류: {ex.Message}");
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"JSON 파싱 오류: {ex.Message}");
                }
            } catch (OperationCanceledException) {
                throw;
            } finally {
                process?.Dispose();
            }

            return subtitles;
        }

        private string GetLanguageName(string languageCode) {
            if (string.IsNullOrEmpty(languageCode)) {
                return "알 수 없음";
            }

            // 주요 언어 코드를 한글 이름으로 매핑
            Dictionary<string, string> languageMap = new Dictionary<string, string> {
                { "ko", "한국어" },
                { "en", "영어" },
                { "ja", "일본어" },
                { "zh", "중국어" },
                { "zh-Hans", "중국어 (간체)" },
                { "zh-Hant", "중국어 (번체)" },
                { "es", "스페인어" },
                { "fr", "프랑스어" },
                { "de", "독일어" },
                { "ru", "러시아어" },
                { "pt", "포르투갈어" },
                { "it", "이탈리아어" },
                { "ar", "아랍어" },
                { "hi", "힌디어" },
                { "th", "태국어" },
                { "vi", "베트남어" },
                { "id", "인도네시아어" }
            };

            if (languageMap.ContainsKey(languageCode)) {
                return languageMap[languageCode];
            }

            // 매핑되지 않은 경우 코드 그대로 반환
            return languageCode.ToUpper();
        }

        private void UpdateCombo() {
            try {
                comboBox2.Items.Clear();

                string[] formats = radioButton1.Checked ? Tol.VideoFormats : Tol.AudioFormats;
                comboBox2.Items.AddRange(formats);

                if (videoInfo.TypeSave != null && comboBox2.Items.Contains(videoInfo.TypeSave.SubType)) {
                    comboBox2.SelectedItem = videoInfo.TypeSave.SubType;
                } else if (comboBox2.Items.Count > 0) {
                    comboBox2.SelectedIndex = 0;
                }
            } catch (Exception ex) {
                Console.WriteLine($"UpdateCombo 오류: {ex.Message}");
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            UpdateCombo();
        }

        private void button1_Click(object sender, EventArgs e) {
            try {
                if (videoInfo.TypeSave == null) {
                    videoInfo.TypeSave = new TypeSaveVideo();
                }

                videoInfo.TypeSave.IsTypeVideo = radioButton1.Checked;
                videoInfo.TypeSave.SubType = comboBox2.SelectedItem?.ToString() ?? "mp4";

                if (videoInfo.VideoQualities != null) {
                    foreach (VideoQuality quality in videoInfo.VideoQualities) {
                        quality.IsSelected = false;
                    }

                    VideoQuality selectedQuality = comboBox1.SelectedItem as VideoQuality;
                    if (selectedQuality != null) {
                        selectedQuality.IsSelected = true;
                    }
                }

                MessageBox.Show("다운로드 옵션이 저장되었습니다.", "✅ 저장 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show($"옵션 저장 중 오류가 발생했습니다:\n{ex.Message}",
                    "❌ 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button2_Click(object sender, EventArgs e) {
            SubtitleInfo subtitleInfo = comboBox3.SelectedItem as SubtitleInfo;
            if (subtitleInfo == null) {
                MessageBox.Show("자막을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CancellationTokenSource cts = new CancellationTokenSource();

            try {
                button2.Enabled = false;
                button2.Text = "⏳ 다운로드 중...";

                string sanitizedTitle = string.Concat((videoInfo.Title ?? "subtitle").Split(Path.GetInvalidFileNameChars()));

                if (string.IsNullOrWhiteSpace(sanitizedTitle)) {
                    sanitizedTitle = "subtitle_" + videoInfo.ID;
                }

                string savePath = Settings.Default.Path;
                if (string.IsNullOrEmpty(savePath) || !Directory.Exists(savePath)) {
                    savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                string subtitlePath = Path.Combine(savePath, $"{sanitizedTitle}-{subtitleInfo.LanguageCode}.srt");

                // 경로 길이 체크
                if (subtitlePath.Length > 250) {
                    sanitizedTitle = sanitizedTitle.Substring(0, Math.Min(sanitizedTitle.Length, 50));
                    subtitlePath = Path.Combine(savePath, $"{sanitizedTitle}-{subtitleInfo.LanguageCode}.srt");
                }

                // yt-dlp로 자막 다운로드
                await DownloadSubtitleWithYtDlp(videoUrl, subtitleInfo.LanguageCode, subtitlePath, cts.Token);

                var result = MessageBox.Show(
                    $"자막이 성공적으로 다운로드되었습니다.\n\n저장 위치: {subtitlePath}\n\n폴더를 열시겠습니까?",
                    "✅ 다운로드 완료",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes) {
                    try {
                        Process.Start("explorer.exe", $"/select,\"{subtitlePath}\"");
                    } catch (Exception ex) {
                        Console.WriteLine($"탐색기 열기 실패: {ex.Message}");
                        MessageBox.Show($"파일이 저장되었지만 탐색기를 열 수 없습니다.\n경로: {subtitlePath}",
                            "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            } catch (OperationCanceledException) {
                MessageBox.Show("자막 다운로드가 취소되었습니다.",
                    "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show($"자막 다운로드 중 오류가 발생했습니다:\n{ex.Message}",
                    "❌ 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                button2.Enabled = true;
                button2.Text = "⬇️ 자막 다운로드";
                cts?.Dispose();
            }
        }

        private async Task DownloadSubtitleWithYtDlp(string url, string languageCode, string outputPath, CancellationToken cancellationToken) {
            string ytdlpPath = Path.Combine(Tol.AppdataPath, "yt-dlp.exe");

            if (!File.Exists(ytdlpPath)) {
                throw new Exception("yt-dlp.exe를 찾을 수 없습니다.");
            }

            // 출력 파일의 확장자 제거 (yt-dlp가 자동으로 .srt 추가)
            string outputTemplate = Path.Combine(
                Path.GetDirectoryName(outputPath),
                Path.GetFileNameWithoutExtension(outputPath)
            );

            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = ytdlpPath,
                Arguments = $"--write-sub --sub-lang {languageCode} --skip-download --sub-format srt --convert-subs srt -o \"{outputTemplate}.%(ext)s\" \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            Process process = null;
            try {
                process = new Process { StartInfo = startInfo };
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        output.AppendLine(e.Data);
                        Console.WriteLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data)) {
                        error.AppendLine(e.Data);
                        Console.WriteLine($"ERROR: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => {
                    while (!process.WaitForExit(1000)) {
                        if (cancellationToken.IsCancellationRequested) {
                            try {
                                process.Kill();
                            } catch { }
                            throw new OperationCanceledException();
                        }
                    }
                }, cancellationToken);

                if (process.ExitCode != 0) {
                    string errorMsg = error.ToString();
                    if (errorMsg.Contains("not available")) {
                        throw new Exception("해당 언어의 자막을 사용할 수 없습니다.");
                    } else {
                        throw new Exception($"자막 다운로드 실패: {errorMsg}");
                    }
                }

                // yt-dlp가 생성한 파일 찾기
                string[] possibleFiles = new string[] {
                    $"{outputTemplate}.{languageCode}.srt",
                    $"{outputTemplate}.srt",
                    outputPath
                };

                string actualFile = possibleFiles.FirstOrDefault(f => File.Exists(f));

                if (actualFile == null) {
                    throw new Exception("자막 파일을 찾을 수 없습니다.");
                }

                // 원하는 파일명으로 이동
                if (actualFile != outputPath && File.Exists(actualFile)) {
                    if (File.Exists(outputPath)) {
                        File.Delete(outputPath);
                    }
                    File.Move(actualFile, outputPath);
                }
            } catch (OperationCanceledException) {
                throw;
            } finally {
                process?.Dispose();
            }
        }

        // URL 복사 버튼
        private void btnCopyUrl_Click(object sender, EventArgs e) {
            try {
                if (string.IsNullOrEmpty(videoUrl)) {
                    MessageBox.Show("URL이 없습니다.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Clipboard.SetText(videoUrl);

                // 버튼 텍스트 임시 변경으로 피드백
                string originalText = btnCopyUrl.Text;
                btnCopyUrl.Text = "✅ 복사됨!";
                btnCopyUrl.Enabled = false;

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = 1500;
                timer.Tick += (s, args) => {
                    try {
                        if (!isClosing && btnCopyUrl != null && !btnCopyUrl.IsDisposed) {
                            btnCopyUrl.Text = originalText;
                            btnCopyUrl.Enabled = true;
                        }
                    } catch { }
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            } catch (Exception ex) {
                MessageBox.Show($"URL 복사 중 오류가 발생했습니다:\n{ex.Message}",
                    "❌ 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // YouTube에서 열기 버튼
        private void btnOpenUrl_Click(object sender, EventArgs e) {
            try {
                if (string.IsNullOrEmpty(videoUrl)) {
                    MessageBox.Show("URL이 없습니다.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo {
                    FileName = videoUrl,
                    UseShellExecute = true
                });
            } catch (Exception ex) {
                MessageBox.Show($"URL을 여는 중 오류가 발생했습니다:\n{ex.Message}",
                    "❌ 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 닫기 버튼
        private void btnClose_Click(object sender, EventArgs e) {
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            isClosing = true;
            subtitleLoadCts?.Cancel();
            subtitleLoadCts?.Dispose();
            base.OnFormClosing(e);
        }

        // 버튼 호버 효과
        private void Button_MouseEnter(object sender, EventArgs e) {
            try {
                if (sender is Button btn && btn.Enabled) {
                    Color originalColor = btn.BackColor;
                    int r = Math.Min(255, originalColor.R + 20);
                    int g = Math.Min(255, originalColor.G + 20);
                    int b = Math.Min(255, originalColor.B + 20);
                    btn.BackColor = Color.FromArgb(r, g, b);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Button_MouseEnter 오류: {ex.Message}");
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e) {
            try {
                if (sender is Button btn) {
                    // 원래 색상으로 복원
                    if (btn.Name == "button1") {
                        btn.BackColor = Color.FromArgb(46, 204, 113); // 녹색 - 옵션 저장
                    } else if (btn.Name == "button2") {
                        btn.BackColor = Color.FromArgb(155, 89, 182); // 보라색 - 자막 다운로드
                    } else if (btn.Name == "btnCopyUrl") {
                        btn.BackColor = Color.FromArgb(52, 152, 219); // 파란색 - URL 복사
                    } else if (btn.Name == "btnOpenUrl") {
                        btn.BackColor = Color.FromArgb(230, 126, 34); // 주황색 - URL 열기
                    } else if (btn.Name == "btnClose") {
                        btn.BackColor = Color.FromArgb(127, 140, 141); // 회색 - 닫기
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Button_MouseLeave 오류: {ex.Message}");
            }
        }
    }

    // 자막 정보 클래스
    public class SubtitleInfo {
        public string LanguageCode { get; set; }
        public string LanguageName { get; set; }
        public bool IsAutoGenerated { get; set; }

        public string DisplayName {
            get {
                string suffix = IsAutoGenerated ? " (자동 생성)" : "";
                return $"{LanguageName}{suffix}";
            }
        }

        public override string ToString() {
            return DisplayName;
        }
    }
}