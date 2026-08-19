using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

using GMTFV.services;

namespace GMTFV.Start {
    /// <summary>
    /// 목록 불러오기 중 부모 폼 중앙에 표시되는 모달 팝업 창
    /// </summary>
    public class ImportProgressForm : DevForm {
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnCancel;
        private Label titleLabel;
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
            this.Size = new Size(460, 210);
            this.ControlBox = false; // 상단 X 닫기 버튼 제거로 무단 닫기 방지
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = FormTheme.Surface;

            titleLabel = new Label {
                Location = new Point(24, 18),
                Size = new Size(400, 26),
                Text = "목록을 준비하고 있어요",
                Font = new Font("맑은 고딕", 13F, FontStyle.Bold),
                ForeColor = FormTheme.Text
            };

            lblStatus = new Label {
                Location = new Point(24, 52),
                Size = new Size(400, 42),
                Text = $"📥 영상 정보 분석 중... (0/{totalCount})",
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Regular)
            };

            progressBar = new ProgressBar {
                Location = new Point(24, 100),
                Size = new Size(400, 18),
                Minimum = 0,
                Maximum = Math.Max(totalCount, 1),
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            btnCancel = new Button {
                Location = new Point(160, 138),
                Size = new Size(140, 36),
                Text = "가져오기 취소",
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

            this.Controls.Add(titleLabel);
            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnCancel);
        }
    }
}
