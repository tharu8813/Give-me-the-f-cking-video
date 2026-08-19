using GMTFV.tools;
using System;
using System.Drawing;
using System.Windows.Forms;

using GMTFV.services;

namespace GMTFV.Start {
    public partial class AddURL : DevForm {
        public string Result { get; set; }

        public AddURL() {
            InitializeComponent();
            headerTitle.Text = "새 영상 추가";
            label1.Text = "YouTube 동영상 주소를 입력하세요";
            hintLabel.Text = "예: youtube.com/watch?v=... · Ctrl+V로 붙여넣을 수 있어요";
            button1.Text = "미리보기";
            button2.Text = "다운로드 목록에 추가";
            AcceptButton = button2;
            CancelButton = null;
            textBox1.Focus();
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
                if (btn == button2) btn.BackColor = FormTheme.Primary;
                else if (btn == button1) btn.BackColor = FormTheme.Secondary;
            }
        }
    }
}
