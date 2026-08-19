using GMTFV.Properties;
using GMTFV.tools;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using GMTFV.services;

namespace GMTFV.Start {
    public partial class Setting : GMTFV.DevForm {

        // ─────────────────────────────────────────────
        //  필드
        // ─────────────────────────────────────────────

        /// <summary>
        /// 설정 창을 열 때 MainForm의 다운로드 목록에 존재하는 영상 목록.
        /// </summary>
        private readonly List<VideoInfo> _existingVideos;
        private readonly MainForm _mainForm;

        /// <summary>
        /// 다운로드 형식을 목록의 모든 영상에도 일괄 적용할지 여부.
        /// </summary>
        public bool FormatChanged { get; private set; }

        // ─────────────────────────────────────────────
        //  생성자
        // ─────────────────────────────────────────────

        public Setting(List<VideoInfo> existingVideos = null, MainForm mainForm = null) {
            InitializeComponent();
            FormTheme.Apply(this, headerPanel);
            FormTheme.PrimaryButton(button2);
            FormTheme.OutlineButton(button3);
            FormTheme.OutlineButton(btnExport);
            FormTheme.OutlineButton(btnImport);
            FormTheme.OutlineButton(btnReset);
            headerTitle.Text = "다운로드 설정";
            tabBasic.Text = "기본";
            tabAdvanced.Text = "고급";
            button2.Text = "변경사항 저장";
            btnExport.Text = "목록 내보내기";
            btnImport.Text = "목록 불러오기";
            _existingVideos = existingVideos ?? new List<VideoInfo>();
            _mainForm = mainForm;
        }

        private void btnExport_Click(object sender, EventArgs e) {
            _mainForm?.ExportList();
        }

        private async void btnImport_Click(object sender, EventArgs e) {
            if (_mainForm != null) {
                await _mainForm.ImportListAsync();
            }
        }

        // ─────────────────────────────────────────────
        //  폼 이벤트
        // ─────────────────────────────────────────────

        private void Setting_Load(object sender, EventArgs e) {
            // 저장 경로
            textBox2.Text = Settings.Default.Path;

            // 다운로드 형식 타입
            if (Settings.Default.IsTypeVideo) {
                radioButton1.Checked = true;
            } else {
                radioButton2.Checked = true;
            }

            UpdateFormatList();

            // 파일명 템플릿
            string template = Settings.Default.FileNameTemplate;
            if (string.IsNullOrWhiteSpace(template))
                template = "%title%_%date%";
            txtFileNameTemplate.Text = template;

            // 고급 설정 - 디자이너 컨트롤 값 로드
            LoadAdvancedSettings();

            UpdatePreview();
        }

        /// <summary>
        /// 디자이너에서 생성된 고급 설정 컨트롤의 값을 로드합니다.
        /// </summary>
        private void LoadAdvancedSettings() {
            // GPU 가속기 로드
            string currentGPU = Settings.Default.GPUAccelerator;
            int gpuIndex = 0;
            if (currentGPU == "NVIDIA") {
                gpuIndex = 1;
            } else if (currentGPU == "AMD") {
                gpuIndex = 2;
            } else if (currentGPU == "Intel") {
                gpuIndex = 3;
            }
            comboGPU.SelectedIndex = gpuIndex;

            // 오디오 비트레이트 로드
            numAudio.Value = Settings.Default.AudioBitrate;

            // 동시 다운로드 수 로드
            numConcurrent.Value = Settings.Default.MaxConcurrentDownloads;
        }


        /// <summary>
        /// 모든 설정을 기본값으로 초기화합니다.
        /// </summary>
        private void btnReset_Click(object sender, EventArgs e) {
            var result = MessageBox.Show(
                "모든 설정을 기본값으로 초기화하시겠습니까?",
                "기본값 초기화",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            // UI 업데이트
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            textBox2.Text = Directory.Exists(downloadsPath) ? downloadsPath : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            radioButton1.Checked = true;
            UpdateFormatList();
            txtFileNameTemplate.Text = "%title%_%date%";

            // 고급 설정 초기화
            comboGPU.SelectedIndex = 0;
            numAudio.Value = 192;
            numConcurrent.Value = 1;

            UpdatePreview();
            MessageBox.Show("기본값으로 초기화되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Setting_FormClosing(object sender, FormClosingEventArgs e) {
            // 이미 저장 처리(DialogResult.OK)로 닫히는 경우는 통과
            if (this.DialogResult == DialogResult.OK) return;

            if (HasChanges() && Tol.ShowQ("변경사항이 있습니다. 저장하시겠습니까?")) {
                if (ValidateSettings()) {
                    CheckAndConfirmFormatChange();
                    SaveSetting();
                } else {
                    e.Cancel = true; // 검증 실패 시 폼 닫기 취소
                }
            }
        }

        // ─────────────────────────────────────────────
        //  컨트롤 이벤트
        // ─────────────────────────────────────────────

        /// <summary>
        /// 저장 경로 찾아보기 버튼
        /// </summary>
        private void button3_Click(object sender, EventArgs e) {
            using (var dialog = new CommonOpenFileDialog()) {
                dialog.IsFolderPicker    = true;
                dialog.InitialDirectory  = Directory.Exists(Settings.Default.Path)
                    ? Settings.Default.Path
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                    textBox2.Text = dialog.FileName;
                }
            }
        }

        /// <summary>
        /// 비디오/오디오 라디오버튼 변경 시 콤보박스 목록과 미리보기를 갱신합니다.
        /// (팝업은 선택할 때가 아닌 저장할 때 표시됩니다)
        /// </summary>
        private void radioButton_CheckedChanged(object sender, EventArgs e) {
            if (!(sender is RadioButton rb) || !rb.Checked) return;
            UpdateFormatList();
            UpdatePreview();
        }

        /// <summary>
        /// 포맷 콤보박스 선택 변경 시 미리보기를 갱신합니다.
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            UpdatePreview();
        }

        /// <summary>
        /// 설정 저장 버튼
        /// </summary>
        private void button2_Click(object sender, EventArgs e) {
            if (ValidateSettings()) {
                CheckAndConfirmFormatChange();
                SaveSetting();
            }
        }

        /// <summary>
        /// 파일명 템플릿 실시간 미리보기
        /// </summary>
        private void TxtFileNameTemplate_TextChanged(object sender, EventArgs e) {
            UpdatePreview();
        }

        // ─────────────────────────────────────────────
        //  내부 로직
        // ─────────────────────────────────────────────

        /// <summary>
        /// 선택된 형식 타입(비디오/오디오)에 맞춰 콤보박스를 갱신합니다.
        /// </summary>
        private void UpdateFormatList() {
            string currentSelection = comboBox1.SelectedItem?.ToString() ?? Settings.Default.SubType;
            comboBox1.Items.Clear();

            string[] formats = radioButton1.Checked ? Tol.VideoFormats : Tol.AudioFormats;
            comboBox1.Items.AddRange(formats);

            if (comboBox1.Items.Count > 0) {
                int index = comboBox1.Items.IndexOf(currentSelection);
                comboBox1.SelectedIndex = index >= 0 ? index : 0;
            }
        }

        /// <summary>
        /// 선택된 형식과 다운로드 목록 중 1개라도 다른 형식이 존재하는 경우
        /// 목록 전체 적용 여부를 팝업으로 묻습니다.
        /// "예" 선택 시 목록도 함께 변경(FormatChanged = true),
        /// "아니오" 선택 시 목록은 유지하고 설정만 저장(FormatChanged = false).
        /// </summary>
        private void CheckAndConfirmFormatChange() {
            bool isTypeVideoNew = radioButton1.Checked;
            string subTypeNew    = comboBox1.SelectedItem?.ToString() ?? "";

            // 목록 중 새로 선택된 형식과 다른 형식을 가진 항목이 1개라도 있는지 확인
            bool hasDifferentItem = _existingVideos != null && _existingVideos.Any(v =>
                v.TypeSave == null ||
                v.TypeSave.IsTypeVideo != isTypeVideoNew ||
                v.TypeSave.SubType != subTypeNew
            );

            if (hasDifferentItem) {
                string typeText = isTypeVideoNew ? "비디오" : "오디오";
                string msg = $"현재 다운로드 목록에 설정과 다른 형식을 가진 영상이 포함되어 있습니다.\n\n" +
                             $"목록의 모든 영상 형식을 [{typeText} / {subTypeNew}](으)로 일괄 변경하시겠습니까?";

                var dialogResult = MessageBox.Show(msg, "다운로드 형식 일괄 적용",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                FormatChanged = (dialogResult == DialogResult.Yes);
            } else {
                FormatChanged = false;
            }
        }

        /// <summary>
        /// 미리보기 레이블을 현재 템플릿 기준으로 갱신합니다.
        /// </summary>
        private void UpdatePreview() {
            try {
                string ext      = comboBox1.SelectedItem?.ToString() ?? "mp4";
                string preview  = BuildPreviewName(txtFileNameTemplate.Text, ext);
                lblPreview.Text = preview + "." + ext;
                lblPreview.ForeColor = Color.FromArgb(39, 174, 96);
            } catch {
                lblPreview.Text      = "(미리보기 오류)";
                lblPreview.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// 샘플 데이터로 파일명 미리보기 문자열을 생성합니다.
        /// </summary>
        private static string BuildPreviewName(string template, string ext) {
            if (string.IsNullOrWhiteSpace(template))
                template = "%title%_%date%";

            return template
                .Replace("%num3%",   "001")
                .Replace("%num2%",   "01")
                .Replace("%num%",    "1")
                .Replace("%no%",     "1")
                .Replace("%index%",  "1")
                .Replace("%title%",  "영상제목_샘플")
                .Replace("%author%", "채널이름")
                .Replace("%date%",   DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("%id%",     "dQw4w9WgXcQ")
                .Replace("%ext%",    ext);
        }

        /// <summary>
        /// 저장 전 입력 값의 유효성을 검사합니다.
        /// </summary>
        private bool ValidateSettings() {
            // 저장 경로 검사
            if (string.IsNullOrWhiteSpace(textBox2.Text)) {
                MessageBox.Show("저장 경로를 선택해주세요.", "경고",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Directory.Exists(textBox2.Text)) {
                var result = MessageBox.Show("존재하지 않는 경로입니다. 계속하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return false;
            }

            // 파일명 템플릿 검사
            string tmpl = txtFileNameTemplate.Text.Trim();
            if (string.IsNullOrWhiteSpace(tmpl)) {
                MessageBox.Show("파일명 형식이 비어있습니다.\n기본값(%title%_%date%)을 사용하거나 직접 입력해주세요.",
                    "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFileNameTemplate.Focus();
                return false;
            }

            // 파일명 금지 문자 검사 (% 변수 바깥의 실제 금지 문자)
            char[] forbiddenChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            string expanded = tmpl
                .Replace("%num3%", "N").Replace("%num2%", "N").Replace("%num%", "N")
                .Replace("%no%", "N").Replace("%index%", "N")
                .Replace("%title%", "T").Replace("%author%", "A")
                .Replace("%date%", "D").Replace("%id%", "I").Replace("%ext%", "E");
            foreach (char c in forbiddenChars) {
                if (expanded.IndexOf(c) >= 0) {
                    MessageBox.Show($"파일명 형식에 사용할 수 없는 문자가 포함되어 있습니다: {c}",
                        "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFileNameTemplate.Focus();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 현재 UI 값을 설정에 저장하고 폼을 닫습니다.
        /// </summary>
        private void SaveSetting() {
            Settings.Default.SubType          = comboBox1.SelectedItem?.ToString() ?? "";
            Settings.Default.Path             = textBox2.Text;
            Settings.Default.IsTypeVideo      = radioButton1.Checked;
            Settings.Default.FileNameTemplate = txtFileNameTemplate.Text.Trim();

            // 고급 설정 저장 - 디자이너 컨트롤에서 직접 읽기
            int gpuIndex = comboGPU.SelectedIndex;
            string gpuValue = "CPU";
            if (gpuIndex == 1) {
                gpuValue = "NVIDIA";
            } else if (gpuIndex == 2) {
                gpuValue = "AMD";
            } else if (gpuIndex == 3) {
                gpuValue = "Intel";
            }
            Settings.Default.GPUAccelerator = gpuValue;

            Settings.Default.AudioBitrate = (int)numAudio.Value;
            Settings.Default.MaxConcurrentDownloads = (int)numConcurrent.Value;

            Settings.Default.Save();
            this.DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// 현재 UI 값이 저장된 값과 다른지 여부를 반환합니다.
        /// </summary>
        private bool HasChanges() {
            bool basicChanges = comboBox1.SelectedItem?.ToString() != Settings.Default.SubType ||
                   textBox2.Text                      != Settings.Default.Path    ||
                   radioButton1.Checked               != Settings.Default.IsTypeVideo ||
                   txtFileNameTemplate.Text.Trim()    != Settings.Default.FileNameTemplate;

            // 고급 설정 변경 확인 - 디자이너 컨트롤에서 직접 읽기
            if (!basicChanges) {
                int gpuIndex = comboGPU.SelectedIndex;
                string gpuValue = "CPU";
                if (gpuIndex == 1) {
                    gpuValue = "NVIDIA";
                } else if (gpuIndex == 2) {
                    gpuValue = "AMD";
                } else if (gpuIndex == 3) {
                    gpuValue = "Intel";
                }
                if (gpuValue != Settings.Default.GPUAccelerator)
                    return true;
            }

            if (!basicChanges) {
                if ((int)numAudio.Value != Settings.Default.AudioBitrate)
                    return true;
            }

            if (!basicChanges) {
                if ((int)numConcurrent.Value != Settings.Default.MaxConcurrentDownloads)
                    return true;
            }

            return basicChanges;
        }

        // ─────────────────────────────────────────────
        //  버튼 호버 효과
        // ─────────────────────────────────────────────

        private void Button_MouseEnter(object sender, EventArgs e) {
            if (sender is Button btn) {
                btn.BackColor = ControlPaint.Light(btn.BackColor, 0.1f);
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e) {
            if (sender is Button btn) {
                if (btn == button2) {
                    btn.BackColor = FormTheme.Primary;
                } else if (btn == button3 || btn == btnReset) {
                    btn.BackColor = Color.White;
                }
            }
        }
    }
}
