using GMTFV.Properties;
using GMTFV.tools;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace GMTFV.Start {
    public partial class VideoInfoForm : DevForm {
        private readonly VideoInfo videoInfo;

        public VideoInfoForm(VideoInfo videoInfo) {
            InitializeComponent();
            this.videoInfo = videoInfo;
        }

        private async void VideoInfoForm_Load(object sender, EventArgs e) {
            label1.Text = videoInfo.Title;
            label2.Text = "저자: " + videoInfo.Author;
            label3.Text = $"길이: {videoInfo.VideoLength}";
            label4.Text = $"업로드 일자: {videoInfo.UploadDate}";
            pictureBox1.Image = videoInfo.Image;

            comboBox1.Items.AddRange(videoInfo.VideoQualities.ToArray());

            int selectedIndex = comboBox1.Items.IndexOf(videoInfo.VideoQualities.FirstOrDefault(v => v.IsSelected));
            if (selectedIndex >= 0) {
                comboBox1.SelectedIndex = selectedIndex;
            }

            radioButton1.Checked = videoInfo.TypeSave.IsTypeVideo;
            radioButton2.Checked = !videoInfo.TypeSave.IsTypeVideo;

            UpdateCombo();
            await UpdataCCAsync();
        }

        private async Task UpdataCCAsync() {
            YoutubeClient youtubeClient = new YoutubeClient();
            string videoUrl = "https://youtube.com/watch?v=" + videoInfo.ID;

            ClosedCaptionManifest manifest = await youtubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

            comboBox3.DisplayMember = "Language.Name";
            comboBox3.ValueMember = "Language.Code";
            comboBox3.Items.AddRange(manifest.Tracks.ToArray());
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
            videoInfo.TypeSave.IsTypeVideo = radioButton1.Checked;
            videoInfo.TypeSave.SubType = comboBox2.SelectedItem?.ToString() ?? "";

            foreach (VideoQuality quality in videoInfo.VideoQualities) {
                quality.IsSelected = false;
            }

            VideoQuality selectedQuality = comboBox1.SelectedItem as VideoQuality;
            if (selectedQuality != null) {
                selectedQuality.IsSelected = true;
            }

            MessageBox.Show("설정이 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            Close();
        }

        private async void button2_Click(object sender, EventArgs e) {
            ClosedCaptionTrackInfo trackInfo = comboBox3.SelectedItem as ClosedCaptionTrackInfo;
            if (trackInfo != null) {
                YoutubeClient youtubeClient = new YoutubeClient();
                string sanitizedTitle = string.Concat(videoInfo.Title.Split(Path.GetInvalidFileNameChars()));
                string subtitlePath = Path.Combine(Settings.Default.Path, $"{sanitizedTitle}-{trackInfo.Language.Code}.srt");

                await youtubeClient.Videos.ClosedCaptions.DownloadAsync(trackInfo, subtitlePath);

                MessageBox.Show("자막이 다운로드되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }
    }
}