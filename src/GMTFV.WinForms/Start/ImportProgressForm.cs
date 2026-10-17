using System;
using System.Threading;
using System.Windows.Forms;

namespace GMTFV.Start {
    /// <summary>
    /// 목록 불러오기 중 부모 폼 중앙에 표시되는 모달 팝업 창
    /// </summary>
    public partial class ImportProgressForm : DevForm {
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public ImportProgressForm(int totalCount) {
            InitializeComponent();
            progressBar.Maximum = Math.Max(totalCount, 1);
            lblStatus.Text = $"📥 영상 정보 분석 중... (0/{totalCount})";
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

        private void btnCancel_Click(object sender, EventArgs e) {
            Cts.Cancel();
            lblStatus.Text = "🛑 불러오기 취소 중...";
            btnCancel.Enabled = false;
        }
    }
}
