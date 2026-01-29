using GMTFV.Properties;
using GMTFV.tools;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace GMTFV.Start {
    public partial class VideoInfoForm : DevForm {
        private readonly VideoInfo videoInfo;
        private string videoUrl;

        public VideoInfoForm(VideoInfo videoInfo) {
            InitializeComponent();
            this.videoInfo = videoInfo;
            this.videoUrl = $"https://www.youtube.com/watch?v={videoInfo.ID}";
        }

        private async void VideoInfoForm_Load(object sender, EventArgs e) {
            // 기본 정보 표시
            label1.Text = videoInfo.Title;
            label2.Text = $"👤 저자: {videoInfo.Author}";
            label3.Text = $"⏱️ 길이: {videoInfo.VideoLength}";
            label4.Text = $"📅 업로드 일자: {videoInfo.UploadDate:yyyy-MM-dd}";
            pictureBox1.Image = videoInfo.Image;

            // URL 표시
            txtUrl.Text = videoUrl;

            // 화질 옵션 설정
            comboBox1.Items.AddRange(videoInfo.VideoQualities.ToArray());
            int selectedIndex = comboBox1.Items.IndexOf(videoInfo.VideoQualities.FirstOrDefault(v => v.IsSelected));
            if (selectedIndex >= 0) {
                comboBox1.SelectedIndex = selectedIndex;
            } else if (comboBox1.Items.Count > 0) {
                comboBox1.SelectedIndex = 0;
            }

            // 비디오/오디오 옵션 설정
            radioButton1.Checked = videoInfo.TypeSave.IsTypeVideo;
            radioButton2.Checked = !videoInfo.TypeSave.IsTypeVideo;

            UpdateCombo();

            // 자막 목록 불러오기
            await UpdateCCAsync();
        }

        private async Task UpdateCCAsync() {
            try {
                comboBox3.Items.Clear();
                comboBox3.Items.Add("자막 불러오는 중...");
                comboBox3.SelectedIndex = 0;
                comboBox3.Enabled = false;
                button2.Enabled = false;

                YoutubeClient youtubeClient = new YoutubeClient();
                ClosedCaptionManifest manifest = await youtubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                comboBox3.Items.Clear();

                if (manifest.Tracks.Any()) {
                    comboBox3.DisplayMember = "Language.Name";
                    comboBox3.ValueMember = "Language.Code";
                    comboBox3.Items.AddRange(manifest.Tracks.ToArray());

                    // 한국어 자막 우선 선택
                    var koreanTrack = manifest.Tracks.FirstOrDefault(t => t.Language.Code.StartsWith("ko"));
                    if (koreanTrack != null) {
                        comboBox3.SelectedItem = koreanTrack;
                    } else if (comboBox3.Items.Count > 0) {
                        comboBox3.SelectedIndex = 0;
                    }

                    comboBox3.Enabled = true;
                    button2.Enabled = true;
                } else {
                    comboBox3.Items.Add("사용 가능한 자막이 없습니다");
                    comboBox3.SelectedIndex = 0;
                }
            } catch (Exception ex) {
                comboBox3.Items.Clear();
                comboBox3.Items.Add("자막을 불러올 수 없습니다");
                comboBox3.SelectedIndex = 0;
                Console.WriteLine($"자막 불러오기 오류: {ex.Message}");
            }
        }

        private void UpdateCombo() {
            comboBox2.Items.Clear();

            string[] formats = radioButton1.Checked ? Tol.VideoFormats : Tol.AudioFormats;
            comboBox2.Items.AddRange(formats);

            if (comboBox2.Items.Contains(videoInfo.TypeSave.SubType)) {
                comboBox2.SelectedItem = videoInfo.TypeSave.SubType;
            } else if (comboBox2.Items.Count > 0) {
                comboBox2.SelectedIndex = 0;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            UpdateCombo();
        }

        private void button1_Click(object sender, EventArgs e) {
            try {
                videoInfo.TypeSave.IsTypeVideo = radioButton1.Checked;
                videoInfo.TypeSave.SubType = comboBox2.SelectedItem?.ToString() ?? "";

                foreach (VideoQuality quality in videoInfo.VideoQualities) {
                    quality.IsSelected = false;
                }

                VideoQuality selectedQuality = comboBox1.SelectedItem as VideoQuality;
                if (selectedQuality != null) {
                    selectedQuality.IsSelected = true;
                }

                MessageBox.Show("다운로드 옵션이 저장되었습니다.", "✅ 저장 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) {
                MessageBox.Show($"옵션 저장 중 오류가 발생했습니다:\n{ex.Message}",
                    "❌ 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button2_Click(object sender, EventArgs e) {
            ClosedCaptionTrackInfo trackInfo = comboBox3.SelectedItem as ClosedCaptionTrackInfo;
            if (trackInfo == null) {
                MessageBox.Show("자막을 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try {
                button2.Enabled = false;
                button2.Text = "⏳ 다운로드 중...";

                YoutubeClient youtubeClient = new YoutubeClient();
                string sanitizedTitle = string.Concat(videoInfo.Title.Split(Path.GetInvalidFileNameChars()));

                if (string.IsNullOrWhiteSpace(sanitizedTitle)) {
                    sanitizedTitle = "subtitle_" + videoInfo.ID;
                }

                string savePath = Settings.Default.Path;
                if (string.IsNullOrEmpty(savePath) || !Directory.Exists(savePath)) {
                    savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                string subtitlePath = Path.Combine(savePath, $"{sanitizedTitle}-{trackInfo.Language.Code}.srt");

                await youtubeClient.Videos.ClosedCaptions.DownloadAsync(trackInfo, subtitlePath);

                var result = MessageBox.Show(
                    $"자막이 성공적으로 다운로드되었습니다.\n\n저장 위치: {subtitlePath}\n\n폴더를 열시겠습니까?",
                    "✅ 다운로드 완료",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes) {
                    Process.Start("explorer.exe", $"/select,\"{subtitlePath}\"");
                }
            } catch (Exception ex) {
                MessageBox.Show($"자막 다운로드 중 오류가 발생했습니다:\n{ex.Message}",
                    "❌ 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } finally {
                button2.Enabled = true;
                button2.Text = "⬇️ 자막 다운로드";
            }
        }

        // URL 복사 버튼
        private void btnCopyUrl_Click(object sender, EventArgs e) {
            try {
                Clipboard.SetText(videoUrl);

                // 버튼 텍스트 임시 변경으로 피드백
                string originalText = btnCopyUrl.Text;
                btnCopyUrl.Text = "✅ 복사됨!";
                btnCopyUrl.Enabled = false;

                Timer timer = new Timer();
                timer.Interval = 1500;
                timer.Tick += (s, args) => {
                    btnCopyUrl.Text = originalText;
                    btnCopyUrl.Enabled = true;
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

        // 버튼 호버 효과
        private void Button_MouseEnter(object sender, EventArgs e) {
            if (sender is Button btn && btn.Enabled) {
                Color originalColor = btn.BackColor;
                int r = Math.Min(255, originalColor.R + 20);
                int g = Math.Min(255, originalColor.G + 20);
                int b = Math.Min(255, originalColor.B + 20);
                btn.BackColor = Color.FromArgb(r, g, b);
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e) {
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
        }
    }
}