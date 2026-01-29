using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    partial class VideoInfoForm {
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
            ComponentResourceManager resources = new ComponentResourceManager(typeof(VideoInfoForm));

            this.label1 = new Label();
            this.label2 = new Label();
            this.label3 = new Label();
            this.label4 = new Label();
            this.pictureBox1 = new PictureBox();
            this.groupBox1 = new GroupBox();
            this.button1 = new Button();
            this.comboBox2 = new ComboBox();
            this.radioButton2 = new RadioButton();
            this.radioButton1 = new RadioButton();
            this.label7 = new Label();
            this.label8 = new Label();
            this.label6 = new Label();
            this.label5 = new Label();
            this.comboBox1 = new ComboBox();
            this.label9 = new Label();
            this.comboBox3 = new ComboBox();
            this.button2 = new Button();

            ((ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();

            // 
            // label1
            // 
            this.label1.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new Point(12, 173);
            this.label1.Name = "label1";
            this.label1.Size = new Size(280, 39);
            this.label1.TabIndex = 1;
            this.label1.Text = "제목";

            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new Point(12, 223);
            this.label2.Name = "label2";
            this.label2.Size = new Size(38, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "저자: ";

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new Point(12, 247);
            this.label3.Name = "label3";
            this.label3.Size = new Size(38, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "길이: ";

            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new Point(12, 272);
            this.label4.Name = "label4";
            this.label4.Size = new Size(78, 15);
            this.label4.TabIndex = 2;
            this.label4.Text = "업로드 일자: ";

            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            this.pictureBox1.Location = new Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Size(280, 158);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;

            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.comboBox2);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.comboBox1);
            this.groupBox1.Location = new Point(301, 82);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(323, 205);
            this.groupBox1.TabIndex = 37;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "추가 옵션";

            // 
            // button1
            // 
            this.button1.Location = new Point(6, 177);
            this.button1.Name = "button1";
            this.button1.Size = new Size(311, 22);
            this.button1.TabIndex = 45;
            this.button1.Text = "저장";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new EventHandler(this.button1_Click);

            // 
            // comboBox2
            // 
            this.comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new Point(9, 110);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new Size(135, 23);
            this.comboBox2.TabIndex = 44;

            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new Point(216, 111);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new Size(61, 19);
            this.radioButton2.TabIndex = 43;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "오디오";
            this.radioButton2.UseVisualStyleBackColor = true;

            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new Point(150, 111);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new Size(61, 19);
            this.radioButton1.TabIndex = 42;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "비디오";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new EventHandler(this.radioButton1_CheckedChanged);

            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new Font("맑은 고딕", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.label7.Location = new Point(6, 136);
            this.label7.Name = "label7";
            this.label7.Size = new Size(186, 13);
            this.label7.TabIndex = 41;
            this.label7.Text = "기본값은 설정에서 저장한 값압나다.";

            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label8.Location = new Point(6, 90);
            this.label8.Name = "label8";
            this.label8.Size = new Size(47, 17);
            this.label8.TabIndex = 40;
            this.label8.Text = "확장명";

            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new Font("맑은 고딕", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.label6.Location = new Point(6, 65);
            this.label6.Name = "label6";
            this.label6.Size = new Size(138, 13);
            this.label6.TabIndex = 39;
            this.label6.Text = "기본값은 최고 화질입니다.";

            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label5.Location = new Point(6, 19);
            this.label5.Name = "label5";
            this.label5.Size = new Size(34, 17);
            this.label5.TabIndex = 38;
            this.label5.Text = "화질";

            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new Point(9, 39);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new Size(183, 23);
            this.comboBox1.TabIndex = 37;

            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new Font("맑은 고딕", 9.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label9.Location = new Point(298, 12);
            this.label9.Name = "label9";
            this.label9.Size = new Size(91, 17);
            this.label9.TabIndex = 47;
            this.label9.Text = "자막 다운로드";

            // 
            // comboBox3
            // 
            this.comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new Point(301, 32);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new Size(208, 23);
            this.comboBox3.TabIndex = 46;

            // 
            // button2
            // 
            this.button2.Location = new Point(515, 33);
            this.button2.Name = "button2";
            this.button2.Size = new Size(109, 22);
            this.button2.TabIndex = 46;
            this.button2.Text = "저장";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new EventHandler(this.button2_Click);

            // 
            // VideoInfoForm
            // 
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(636, 303);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.comboBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VideoInfoForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "상세 정보";
            this.Load += new EventHandler(this.VideoInfoForm_Load);

            ((ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private GroupBox groupBox1;
        private Button button1;
        private ComboBox comboBox2;
        private RadioButton radioButton2;
        private RadioButton radioButton1;
        private Label label7;
        private Label label8;
        private Label label6;
        private Label label5;
        private ComboBox comboBox1;
        private Label label9;
        private ComboBox comboBox3;
        private Button button2;
    }
}