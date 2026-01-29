using GMTFV.tools;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    public partial class AddURL : DevForm {
        public string Result { get; set; }

        public AddURL() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (!Tol.IsYouTubeUrl(textBox1.Text)) {
                Tol.ShowError("해당 주소는 유튜브 주소가 아닙니다.");
                return;
            }

            new VideoCheckcs(textBox1.Text).ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e) {
            if (!Tol.IsYouTubeUrl(textBox1.Text)) {
                Tol.ShowError("해당 주소는 유튜브 주소가 아닙니다.");
                return;
            }

            Result = textBox1.Text;
            DialogResult = DialogResult.OK;
            Dispose();
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
                } else if (btn == button1) {
                    btn.BackColor = Color.FromArgb(52, 152, 219);
                }
            }
        }
    }
}