using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    partial class Setting {
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
            this.button2 = new Button();
            this.label5 = new Label();
            this.comboBox1 = new ComboBox();
            this.radioButton2 = new RadioButton();
            this.radioButton1 = new RadioButton();
            this.label1 = new Label();
            this.label4 = new Label();
            this.button3 = new Button();
            this.textBox2 = new TextBox();
            this.label2 = new Label();
            this.label3 = new Label();
            this.SuspendLayout();

            // 
            // button2
            // 
            this.button2.Location = new Point(9, 245);
            this.button2.Name = "button2";
            this.button2.Size = new Size(381, 23);
            this.button2.TabIndex = 34;
            this.button2.Text = "저장";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new EventHandler(this.button2_Click);

            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new Font("맑은 고딕", 15.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label5.Location = new Point(12, 9);
            this.label5.Name = "label5";
            this.label5.Size = new Size(55, 30);
            this.label5.TabIndex = 33;
            this.label5.Text = "설정";

            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new Point(13, 183);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new Size(231, 23);
            this.comboBox1.TabIndex = 32;

            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new Point(329, 184);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new Size(61, 19);
            this.radioButton2.TabIndex = 31;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "오디오";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new EventHandler(this.radioButton2_CheckedChanged);

            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new Point(263, 184);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new Size(61, 19);
            this.radioButton1.TabIndex = 30;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "비디오";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new EventHandler(this.radioButton1_CheckedChanged);

            // 
            // label1
            // 
            this.label1.Font = new Font("맑은 고딕 Semilight", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new Point(14, 147);
            this.label1.Name = "label1";
            this.label1.Size = new Size(377, 33);
            this.label1.TabIndex = 29;
            this.label1.Text = "유튜브 영상을 MP4, MP3등으로 다운받을지 선택합니다. 기본값은 비디오(MP4) 입니다.\r\n";

            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label4.Location = new Point(13, 126);
            this.label4.Name = "label4";
            this.label4.Size = new Size(112, 21);
            this.label4.TabIndex = 28;
            this.label4.Text = "다운로드 형식";

            // 
            // button3
            // 
            this.button3.Location = new Point(315, 88);
            this.button3.Name = "button3";
            this.button3.Size = new Size(74, 23);
            this.button3.TabIndex = 27;
            this.button3.Text = "경로 설정";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new EventHandler(this.button3_Click);

            // 
            // textBox2
            // 
            this.textBox2.Location = new Point(13, 88);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new Size(298, 23);
            this.textBox2.TabIndex = 26;

            // 
            // label2
            // 
            this.label2.Font = new Font("맑은 고딕 Semilight", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.label2.Location = new Point(14, 69);
            this.label2.Name = "label2";
            this.label2.Size = new Size(377, 16);
            this.label2.TabIndex = 18;
            this.label2.Text = "다운로드 할 파일이 저장될 경로입니다. 기본값을 다운로드 폴더입니다.";

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label3.Location = new Point(13, 48);
            this.label3.Name = "label3";
            this.label3.Size = new Size(112, 21);
            this.label3.TabIndex = 15;
            this.label3.Text = "다운로드 경로";

            // 
            // Setting
            // 
            this.ClientSize = new Size(402, 280);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Name = "Setting";
            this.Text = "설정";
            this.FormClosing += new FormClosingEventHandler(this.Setting_FormClosing);
            this.Load += new EventHandler(this.Setting_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label label3;
        private Label label2;
        private Button button3;
        private TextBox textBox2;
        private Label label1;
        private Label label4;
        private RadioButton radioButton1;
        private RadioButton radioButton2;
        private ComboBox comboBox1;
        private Label label5;
        private Button button2;
    }
}