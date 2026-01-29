using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GMTFV.tools;

namespace GMTFV.Start {
    public partial class AddURL : DevForm {
        public string Result { get; set; }

        public AddURL() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (!Tol.IsYouTubeUrl(textBox1.Text)) {
                Tol.ShowError("해당 주소를 유튜브 주소가 아닙니다.");
                return;
            }

            new VideoCheckcs(textBox1.Text).ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e) {
            if (!Tol.IsYouTubeUrl(textBox1.Text)) {
                Tol.ShowError("해당 주소를 유튜브 주소가 아닙니다.");
                return;
            }

            Result = textBox1.Text;
            DialogResult = DialogResult.OK;
            Dispose();
        }
    }
}