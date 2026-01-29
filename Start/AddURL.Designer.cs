using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    partial class AddURL {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(AddURL));

            this.button1 = new Button();
            this.label1 = new Label();
            this.textBox1 = new TextBox();
            this.button2 = new Button();

            this.SuspendLayout();

            // 
            // button1
            // 
            this.button1.Location = new Point(295, 9);
            this.button1.Name = "button1";
            this.button1.Size = new Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "확인하기";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new EventHandler(this.button1_Click);

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new Size(218, 21);
            this.label1.TabIndex = 1;
            this.label1.Text = "추가할 주소를 입력해주세요.";

            // 
            // textBox1
            // 
            this.textBox1.BackColor = Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.textBox1.Location = new Point(12, 33);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Size(358, 23);
            this.textBox1.TabIndex = 2;

            // 
            // button2
            // 
            this.button2.Location = new Point(12, 62);
            this.button2.Name = "button2";
            this.button2.Size = new Size(358, 25);
            this.button2.TabIndex = 3;
            this.button2.Text = "추가";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new EventHandler(this.button2_Click);

            // 
            // AddURL
            // 
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(382, 99);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddURL";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "영상 추가";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Button button1;
        private Label label1;
        private TextBox textBox1;
        private Button button2;
    }
}