using GMTFV.Properties;
using GMTFV.tools;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GMTFV.Start {
    public partial class Setting : GMTFV.DevForm {
        public Setting() {
            InitializeComponent();
        }

        private void Setting_Load(object sender, EventArgs e) {
            textBox2.Text = Settings.Default.Path;

            if (Settings.Default.IsTypeVideo) {
                radioButton1.Checked = true;
            } else {
                radioButton2.Checked = true;
            }

            UpdateList(); // 이미 내부에서 SelectedItem 설정함
        }

        private void UpdateList() {
            comboBox1.Items.Clear();

            string[] formats = radioButton1.Checked ? Tol.VideoFormats : Tol.AudioFormats;
            comboBox1.Items.AddRange(formats);

            // 안전한 선택
            if (comboBox1.Items.Count > 0) {
                string subType = Settings.Default.SubType;
                int index = comboBox1.Items.IndexOf(subType);
                comboBox1.SelectedIndex = index >= 0 ? index : 0;
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            using (var dialog = new CommonOpenFileDialog()) {
                dialog.IsFolderPicker = true;
                dialog.InitialDirectory = Directory.Exists(Settings.Default.Path)
                    ? Settings.Default.Path
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                    textBox2.Text = dialog.FileName;
                }
            }
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e) {
            UpdateList();
        }

        private void button2_Click(object sender, EventArgs e) {
            if (ValidateSettings()) {
                SaveSetting();
            }
        }

        private bool ValidateSettings() {
            // 경로 유효성 검사
            if (string.IsNullOrWhiteSpace(textBox2.Text)) {
                MessageBox.Show("저장 경로를 선택해주세요.", "경고",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Directory.Exists(textBox2.Text)) {
                var result = MessageBox.Show("존재하지 않는 경로입니다. 계속하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }

            return true;
        }

        private void SaveSetting() {
            Settings.Default.SubType = comboBox1.SelectedItem?.ToString() ?? "";
            Settings.Default.Path = textBox2.Text;
            Settings.Default.IsTypeVideo = radioButton1.Checked;
            Settings.Default.Save();
            Close(); // Dispose() 대신 Close() 사용
        }

        private bool HasChanges() {
            return comboBox1.SelectedItem?.ToString() != Settings.Default.SubType ||
                   textBox2.Text != Settings.Default.Path ||
                   radioButton1.Checked != Settings.Default.IsTypeVideo;
        }

        private void Setting_FormClosing(object sender, FormClosingEventArgs e) {
            if (HasChanges() && Tol.ShowQ("변경사항이 있습니다. 저장하시겠습니까?")) {
                if (ValidateSettings()) {
                    SaveSetting();
                } else {
                    e.Cancel = true; // 검증 실패 시 폼 닫기 취소
                }
            }
        }

        // 버튼 호버 효과
        private void Button_MouseEnter(object sender, EventArgs e) {
            if (sender is Button btn) {
                var originalColor = btn.BackColor;
                // 약간 밝게
                btn.BackColor = ControlPaint.Light(originalColor, 0.1f);
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e) {
            if (sender is Button btn) {
                // 원래 색상으로 복원
                if (btn == button2) {
                    btn.BackColor = Color.FromArgb(46, 204, 113);
                } else if (btn == button3) {
                    btn.BackColor = Color.FromArgb(52, 152, 219);
                }
            }
        }
    }
}