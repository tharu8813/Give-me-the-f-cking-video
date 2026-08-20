using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    partial class ImportProgressForm {
        private IContainer components = null;
        private Label titleLabel;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnCancel;

        protected override void Dispose(bool disposing) {
            if (disposing) {
                components?.Dispose();
                Cts.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            this.components = new Container();
            this.titleLabel = new Label();
            this.lblStatus = new Label();
            this.progressBar = new ProgressBar();
            this.btnCancel = new Button();
            this.SuspendLayout();
            // titleLabel
            this.titleLabel.Font = new Font("맑은 고딕", 13F, FontStyle.Bold);
            this.titleLabel.ForeColor = Color.FromArgb(15, 23, 42);
            this.titleLabel.Location = new Point(24, 18);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(400, 26);
            this.titleLabel.Text = "목록을 준비하고 있어요";
            // lblStatus
            this.lblStatus.Font = new Font("맑은 고딕", 9.5F);
            this.lblStatus.ForeColor = Color.FromArgb(71, 85, 105);
            this.lblStatus.Location = new Point(24, 52);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(400, 42);
            this.lblStatus.Text = "📥 영상 정보 분석 중...";
            // progressBar
            this.progressBar.Location = new Point(24, 100);
            this.progressBar.Maximum = 1;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(400, 18);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            // btnCancel
            this.btnCancel.BackColor = Color.FromArgb(220, 38, 38);
            this.btnCancel.Cursor = Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
            this.btnCancel.ForeColor = Color.White;
            this.btnCancel.Location = new Point(160, 138);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(140, 36);
            this.btnCancel.Text = "가져오기 취소";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // ImportProgressForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.ClientSize = new Size(444, 190);
            this.ControlBox = false;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.titleLabel);
            this.Font = new Font("맑은 고딕", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ImportProgressForm";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "목록 불러오기";
            this.ResumeLayout(false);
        }
    }
}
