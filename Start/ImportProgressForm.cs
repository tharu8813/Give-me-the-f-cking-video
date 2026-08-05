using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GMTFV.Start {
    /// <summary>
    /// 목록 불러오기 중 부모 폼 중앙에 표시되는 모달 팝업 창
    /// </summary>
    public class ImportProgressForm : DevForm {
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnCancel;
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public ImportProgressForm(int totalCount) {
            InitializeComponent(totalCount);
        }

        public void UpdateProgress(int current, int total, string currentUrl) {
            if (IsDisposed || !IsHandleCreated) return;
            try {
                Invoke((Action)(() => {
                    string displayUrl = currentUrl ?? "";
                    if (displayUrl.Length > 40) {
                        displayUrl = displayUrl.Substring(0, 37) + "...";
                    }
                    lblStatus.Text = $"📥 영상 정보 분석 중... ({current}/{total})\n{displayUrl}";
                    progressBar.Maximum = Math.Max(total, 1);
                    progressBar.Value = Math.Min(current, progressBar.Maximum);
                }));
            } catch { }
        }

        private void InitializeComponent(int totalCount) {
            this.Text = "목록 불러오기";
            this.Size = new Size(440, 185);
            this.ControlBox = false; // 상단 X 닫기 버튼 제거로 무단 닫기 방지
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            lblStatus = new Label {
                Location = new Point(20, 15),
                Size = new Size(385, 42),
                Text = $"📥 영상 정보 분석 중... (0/{totalCount})",
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Regular)
            };

            progressBar = new ProgressBar {
                Location = new Point(20, 62),
                Size = new Size(385, 22),
                Minimum = 0,
                Maximum = Math.Max(totalCount, 1),
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            btnCancel = new Button {
                Location = new Point(155, 98),
                Size = new Size(120, 34),
                Text = "⏹ 취소",
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold)
            };
            btnCancel.Click += (s, e) => {
                Cts.Cancel();
                lblStatus.Text = "🛑 불러오기 취소 중...";
                btnCancel.Enabled = false;
            };

            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnCancel);
        }
    }
}
