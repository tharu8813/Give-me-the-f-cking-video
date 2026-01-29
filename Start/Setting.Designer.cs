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
            this.headerPanel = new System.Windows.Forms.Panel();
            this.headerTitle = new System.Windows.Forms.Label();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.downloadFormatGroup = new System.Windows.Forms.GroupBox();
            this.formatTypePanel = new System.Windows.Forms.Panel();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.downloadPathGroup = new System.Windows.Forms.GroupBox();
            this.button3 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.headerPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.downloadFormatGroup.SuspendLayout();
            this.formatTypePanel.SuspendLayout();
            this.downloadPathGroup.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.headerPanel.Controls.Add(this.headerTitle);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.headerPanel.Size = new System.Drawing.Size(500, 70);
            this.headerPanel.TabIndex = 0;
            // 
            // headerTitle
            // 
            this.headerTitle.AutoSize = true;
            this.headerTitle.Font = new System.Drawing.Font("맑은 고딕", 18F, System.Drawing.FontStyle.Bold);
            this.headerTitle.ForeColor = System.Drawing.Color.White;
            this.headerTitle.Location = new System.Drawing.Point(20, 20);
            this.headerTitle.Name = "headerTitle";
            this.headerTitle.Size = new System.Drawing.Size(103, 32);
            this.headerTitle.TabIndex = 0;
            this.headerTitle.Text = "⚙️ 설정";
            // 
            // contentPanel
            // 
            this.contentPanel.BackColor = System.Drawing.Color.White;
            this.contentPanel.Controls.Add(this.downloadFormatGroup);
            this.contentPanel.Controls.Add(this.downloadPathGroup);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(0, 70);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Padding = new System.Windows.Forms.Padding(20);
            this.contentPanel.Size = new System.Drawing.Size(500, 310);
            this.contentPanel.TabIndex = 1;
            // 
            // downloadFormatGroup
            // 
            this.downloadFormatGroup.Controls.Add(this.formatTypePanel);
            this.downloadFormatGroup.Controls.Add(this.comboBox1);
            this.downloadFormatGroup.Controls.Add(this.label4);
            this.downloadFormatGroup.Controls.Add(this.label1);
            this.downloadFormatGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.downloadFormatGroup.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.downloadFormatGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.downloadFormatGroup.Location = new System.Drawing.Point(20, 145);
            this.downloadFormatGroup.Name = "downloadFormatGroup";
            this.downloadFormatGroup.Padding = new System.Windows.Forms.Padding(15);
            this.downloadFormatGroup.Size = new System.Drawing.Size(460, 145);
            this.downloadFormatGroup.TabIndex = 1;
            this.downloadFormatGroup.TabStop = false;
            this.downloadFormatGroup.Text = "📥 다운로드 형식";
            // 
            // formatTypePanel
            // 
            this.formatTypePanel.Controls.Add(this.radioButton2);
            this.formatTypePanel.Controls.Add(this.radioButton1);
            this.formatTypePanel.Location = new System.Drawing.Point(245, 75);
            this.formatTypePanel.Name = "formatTypePanel";
            this.formatTypePanel.Size = new System.Drawing.Size(200, 30);
            this.formatTypePanel.TabIndex = 2;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.radioButton2.Location = new System.Drawing.Point(100, 5);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(77, 19);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Text = "🎵 오디오";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.radioButton1.Location = new System.Drawing.Point(0, 5);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(77, 19);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "🎥 비디오";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(18, 77);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(215, 23);
            this.comboBox1.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold);
            this.label4.Location = new System.Drawing.Point(15, 55);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 17);
            this.label4.TabIndex = 0;
            this.label4.Text = "📄 형식";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("맑은 고딕", 8.25F);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.label1.Location = new System.Drawing.Point(15, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(430, 28);
            this.label1.TabIndex = 3;
            this.label1.Text = "💡 유튜브 영상을 MP4, MP3 등으로 다운받을지 선택합니다.\r\n    기본값은 비디오(MP4)입니다.";
            // 
            // downloadPathGroup
            // 
            this.downloadPathGroup.Controls.Add(this.button3);
            this.downloadPathGroup.Controls.Add(this.textBox2);
            this.downloadPathGroup.Controls.Add(this.label3);
            this.downloadPathGroup.Controls.Add(this.label2);
            this.downloadPathGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.downloadPathGroup.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.downloadPathGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.downloadPathGroup.Location = new System.Drawing.Point(20, 20);
            this.downloadPathGroup.Name = "downloadPathGroup";
            this.downloadPathGroup.Padding = new System.Windows.Forms.Padding(15);
            this.downloadPathGroup.Size = new System.Drawing.Size(460, 125);
            this.downloadPathGroup.TabIndex = 0;
            this.downloadPathGroup.TabStop = false;
            this.downloadPathGroup.Text = "📁 다운로드 경로";
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.button3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button3.FlatAppearance.BorderSize = 0;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.button3.ForeColor = System.Drawing.Color.White;
            this.button3.Location = new System.Drawing.Point(345, 75);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(100, 28);
            this.button3.TabIndex = 2;
            this.button3.Text = "🔍 찾아보기";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            this.button3.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button3.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.Color.White;
            this.textBox2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.textBox2.Location = new System.Drawing.Point(18, 76);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(320, 23);
            this.textBox2.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(15, 55);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "💾 저장 위치";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("맑은 고딕", 8.25F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.label2.Location = new System.Drawing.Point(15, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(430, 28);
            this.label2.TabIndex = 3;
            this.label2.Text = "💡 다운로드할 파일이 저장될 경로입니다.\r\n    기본값은 다운로드 폴더입니다.";
            // 
            // bottomPanel
            // 
            this.bottomPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.bottomPanel.Controls.Add(this.button2);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 380);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Padding = new System.Windows.Forms.Padding(20, 10, 20, 15);
            this.bottomPanel.Size = new System.Drawing.Size(500, 60);
            this.bottomPanel.TabIndex = 2;
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.button2.ForeColor = System.Drawing.Color.White;
            this.button2.Location = new System.Drawing.Point(20, 10);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(460, 35);
            this.button2.TabIndex = 0;
            this.button2.Text = "💾 설정 저장";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            this.button2.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button2.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            // 
            // Setting
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(500, 440);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.bottomPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Setting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "설정";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Setting_FormClosing);
            this.Load += new System.EventHandler(this.Setting_Load);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.contentPanel.ResumeLayout(false);
            this.downloadFormatGroup.ResumeLayout(false);
            this.downloadFormatGroup.PerformLayout();
            this.formatTypePanel.ResumeLayout(false);
            this.formatTypePanel.PerformLayout();
            this.downloadPathGroup.ResumeLayout(false);
            this.downloadPathGroup.PerformLayout();
            this.bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Panel headerPanel;
        private Label headerTitle;
        private Panel contentPanel;
        private GroupBox downloadPathGroup;
        private Button button3;
        private TextBox textBox2;
        private Label label3;
        private Label label2;
        private GroupBox downloadFormatGroup;
        private Panel formatTypePanel;
        private RadioButton radioButton2;
        private RadioButton radioButton1;
        private ComboBox comboBox1;
        private Label label4;
        private Label label1;
        private Panel bottomPanel;
        private Button button2;
    }
}