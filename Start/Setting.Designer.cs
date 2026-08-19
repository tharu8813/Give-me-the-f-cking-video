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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabBasic = new System.Windows.Forms.TabPage();
            this.downloadPathGroup = new System.Windows.Forms.GroupBox();
            this.button3 = new System.Windows.Forms.Button();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.downloadFormatGroup = new System.Windows.Forms.GroupBox();
            this.formatTypePanel = new System.Windows.Forms.Panel();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.fileNameGroup = new System.Windows.Forms.GroupBox();
            this.lblPreviewCaption = new System.Windows.Forms.Label();
            this.lblPreview = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.txtFileNameTemplate = new System.Windows.Forms.TextBox();
            this.lblTemplateTitle = new System.Windows.Forms.Label();
            this.lblFileNameDesc = new System.Windows.Forms.Label();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.advancedGroupBox = new System.Windows.Forms.GroupBox();
            this.labelConcurrentDesc = new System.Windows.Forms.Label();
            this.numConcurrent = new System.Windows.Forms.NumericUpDown();
            this.labelConcurrent = new System.Windows.Forms.Label();
            this.labelAudioDesc = new System.Windows.Forms.Label();
            this.numAudio = new System.Windows.Forms.NumericUpDown();
            this.labelAudio = new System.Windows.Forms.Label();
            this.labelGPUDesc = new System.Windows.Forms.Label();
            this.comboGPU = new System.Windows.Forms.ComboBox();
            this.labelGPU = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.headerPanel.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabBasic.SuspendLayout();
            this.downloadPathGroup.SuspendLayout();
            this.downloadFormatGroup.SuspendLayout();
            this.formatTypePanel.SuspendLayout();
            this.fileNameGroup.SuspendLayout();
            this.tabAdvanced.SuspendLayout();
            this.advancedGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numConcurrent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAudio)).BeginInit();
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
            this.headerPanel.Size = new System.Drawing.Size(600, 70);
            this.headerPanel.TabIndex = 0;
            // 
            // headerTitle
            // 
            this.headerTitle.AutoSize = true;
            this.headerTitle.Font = new System.Drawing.Font("맑은 고딕", 18F, System.Drawing.FontStyle.Bold);
            this.headerTitle.ForeColor = System.Drawing.Color.White;
            this.headerTitle.Location = new System.Drawing.Point(20, 20);
            this.headerTitle.Name = "headerTitle";
            this.headerTitle.Size = new System.Drawing.Size(104, 32);
            this.headerTitle.TabIndex = 0;
            this.headerTitle.Text = "⚙️ 설정";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabBasic);
            this.tabControl1.Controls.Add(this.tabAdvanced);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 70);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(600, 430);
            this.tabControl1.TabIndex = 2;
            // 
            // tabBasic
            // 
            this.tabBasic.AutoScroll = true;
            this.tabBasic.BackColor = System.Drawing.Color.White;
            this.tabBasic.Controls.Add(this.downloadPathGroup);
            this.tabBasic.Controls.Add(this.downloadFormatGroup);
            this.tabBasic.Controls.Add(this.fileNameGroup);
            this.tabBasic.Location = new System.Drawing.Point(4, 24);
            this.tabBasic.Name = "tabBasic";
            this.tabBasic.Padding = new System.Windows.Forms.Padding(10);
            this.tabBasic.Size = new System.Drawing.Size(592, 402);
            this.tabBasic.TabIndex = 0;
            this.tabBasic.Text = "기본 설정";
            // 
            // downloadPathGroup
            // 
            this.downloadPathGroup.Controls.Add(this.button3);
            this.downloadPathGroup.Controls.Add(this.textBox2);
            this.downloadPathGroup.Controls.Add(this.label3);
            this.downloadPathGroup.Controls.Add(this.label2);
            this.downloadPathGroup.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.downloadPathGroup.Location = new System.Drawing.Point(10, 10);
            this.downloadPathGroup.Name = "downloadPathGroup";
            this.downloadPathGroup.Size = new System.Drawing.Size(552, 100);
            this.downloadPathGroup.TabIndex = 0;
            this.downloadPathGroup.TabStop = false;
            this.downloadPathGroup.Text = "📁 저장 경로";
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.button3.ForeColor = System.Drawing.Color.White;
            this.button3.Location = new System.Drawing.Point(460, 45);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 27);
            this.button3.TabIndex = 3;
            this.button3.Text = "찾아보기";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(15, 45);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(439, 25);
            this.textBox2.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.label3.ForeColor = System.Drawing.Color.Gray;
            this.label3.Location = new System.Drawing.Point(15, 75);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(238, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "💡 다운로드 파일이 저장될 폴더를 선택하세요.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.label2.Location = new System.Drawing.Point(15, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 19);
            this.label2.TabIndex = 0;
            this.label2.Text = "저장 위치:";
            // 
            // downloadFormatGroup
            // 
            this.downloadFormatGroup.Controls.Add(this.formatTypePanel);
            this.downloadFormatGroup.Controls.Add(this.comboBox1);
            this.downloadFormatGroup.Controls.Add(this.label4);
            this.downloadFormatGroup.Controls.Add(this.label1);
            this.downloadFormatGroup.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.downloadFormatGroup.Location = new System.Drawing.Point(10, 120);
            this.downloadFormatGroup.Name = "downloadFormatGroup";
            this.downloadFormatGroup.Size = new System.Drawing.Size(552, 110);
            this.downloadFormatGroup.TabIndex = 1;
            this.downloadFormatGroup.TabStop = false;
            this.downloadFormatGroup.Text = "📥 다운로드 형식";
            // 
            // formatTypePanel
            // 
            this.formatTypePanel.Controls.Add(this.radioButton2);
            this.formatTypePanel.Controls.Add(this.radioButton1);
            this.formatTypePanel.Location = new System.Drawing.Point(15, 25);
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
            this.radioButton2.Text = "🔊 오디오";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.radioButton1.Location = new System.Drawing.Point(5, 5);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(77, 19);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "🎬 비디오";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(87, 60);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(100, 25);
            this.comboBox1.TabIndex = 1;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.label4.ForeColor = System.Drawing.Color.Gray;
            this.label4.Location = new System.Drawing.Point(15, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(153, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "💡 원하는 형식을 선택하세요.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.label1.Location = new System.Drawing.Point(14, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "파일 형식:";
            // 
            // fileNameGroup
            // 
            this.fileNameGroup.Controls.Add(this.lblPreviewCaption);
            this.fileNameGroup.Controls.Add(this.lblPreview);
            this.fileNameGroup.Controls.Add(this.lblHint);
            this.fileNameGroup.Controls.Add(this.txtFileNameTemplate);
            this.fileNameGroup.Controls.Add(this.lblTemplateTitle);
            this.fileNameGroup.Controls.Add(this.lblFileNameDesc);
            this.fileNameGroup.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.fileNameGroup.Location = new System.Drawing.Point(10, 240);
            this.fileNameGroup.Name = "fileNameGroup";
            this.fileNameGroup.Size = new System.Drawing.Size(552, 149);
            this.fileNameGroup.TabIndex = 2;
            this.fileNameGroup.TabStop = false;
            this.fileNameGroup.Text = "📝 파일명 템플릿";
            // 
            // lblPreviewCaption
            // 
            this.lblPreviewCaption.AutoSize = true;
            this.lblPreviewCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPreviewCaption.Location = new System.Drawing.Point(15, 105);
            this.lblPreviewCaption.Name = "lblPreviewCaption";
            this.lblPreviewCaption.Size = new System.Drawing.Size(58, 15);
            this.lblPreviewCaption.TabIndex = 5;
            this.lblPreviewCaption.Text = "미리보기:";
            // 
            // lblPreview
            // 
            this.lblPreview.AutoSize = true;
            this.lblPreview.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.lblPreview.Location = new System.Drawing.Point(75, 105);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Size = new System.Drawing.Size(154, 15);
            this.lblPreview.TabIndex = 4;
            this.lblPreview.Text = "영상제목_2025-01-17.mp4";
            // 
            // lblHint
            // 
            this.lblHint.AutoSize = true;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lblHint.ForeColor = System.Drawing.Color.Gray;
            this.lblHint.Location = new System.Drawing.Point(15, 125);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(503, 13);
            this.lblHint.TabIndex = 3;
            this.lblHint.Text = "💡 %title%(제목), %author%(채널명), %date%(날짜), %num2%(01,02...), %num3%(001,002...) 사" +
    "용 가능";
            // 
            // txtFileNameTemplate
            // 
            this.txtFileNameTemplate.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.txtFileNameTemplate.Location = new System.Drawing.Point(15, 70);
            this.txtFileNameTemplate.Name = "txtFileNameTemplate";
            this.txtFileNameTemplate.Size = new System.Drawing.Size(520, 23);
            this.txtFileNameTemplate.TabIndex = 2;
            this.txtFileNameTemplate.TextChanged += new System.EventHandler(this.TxtFileNameTemplate_TextChanged);
            // 
            // lblTemplateTitle
            // 
            this.lblTemplateTitle.AutoSize = true;
            this.lblTemplateTitle.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblTemplateTitle.Location = new System.Drawing.Point(15, 45);
            this.lblTemplateTitle.Name = "lblTemplateTitle";
            this.lblTemplateTitle.Size = new System.Drawing.Size(54, 19);
            this.lblTemplateTitle.TabIndex = 1;
            this.lblTemplateTitle.Text = "템플릿:";
            // 
            // lblFileNameDesc
            // 
            this.lblFileNameDesc.AutoSize = true;
            this.lblFileNameDesc.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lblFileNameDesc.ForeColor = System.Drawing.Color.Gray;
            this.lblFileNameDesc.Location = new System.Drawing.Point(15, 20);
            this.lblFileNameDesc.Name = "lblFileNameDesc";
            this.lblFileNameDesc.Size = new System.Drawing.Size(223, 13);
            this.lblFileNameDesc.TabIndex = 0;
            this.lblFileNameDesc.Text = "다운로드된 파일의 이름 형식을 지정합니다.";
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.AutoScroll = true;
            this.tabAdvanced.BackColor = System.Drawing.Color.White;
            this.tabAdvanced.Controls.Add(this.advancedGroupBox);
            this.tabAdvanced.Controls.Add(this.btnReset);
            this.tabAdvanced.Location = new System.Drawing.Point(4, 24);
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new System.Windows.Forms.Padding(20);
            this.tabAdvanced.Size = new System.Drawing.Size(592, 402);
            this.tabAdvanced.TabIndex = 1;
            this.tabAdvanced.Text = "고급 설정";
            // 
            // advancedGroupBox
            // 
            this.advancedGroupBox.Controls.Add(this.labelConcurrentDesc);
            this.advancedGroupBox.Controls.Add(this.numConcurrent);
            this.advancedGroupBox.Controls.Add(this.labelConcurrent);
            this.advancedGroupBox.Controls.Add(this.labelAudioDesc);
            this.advancedGroupBox.Controls.Add(this.numAudio);
            this.advancedGroupBox.Controls.Add(this.labelAudio);
            this.advancedGroupBox.Controls.Add(this.labelGPUDesc);
            this.advancedGroupBox.Controls.Add(this.comboGPU);
            this.advancedGroupBox.Controls.Add(this.labelGPU);
            this.advancedGroupBox.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.advancedGroupBox.Location = new System.Drawing.Point(20, 20);
            this.advancedGroupBox.Name = "advancedGroupBox";
            this.advancedGroupBox.Size = new System.Drawing.Size(552, 300);
            this.advancedGroupBox.TabIndex = 0;
            this.advancedGroupBox.TabStop = false;
            this.advancedGroupBox.Text = "⚡ 성능 및 최적화";
            // 
            // labelConcurrentDesc
            // 
            this.labelConcurrentDesc.AutoSize = true;
            this.labelConcurrentDesc.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.labelConcurrentDesc.ForeColor = System.Drawing.Color.Gray;
            this.labelConcurrentDesc.Location = new System.Drawing.Point(180, 180);
            this.labelConcurrentDesc.Name = "labelConcurrentDesc";
            this.labelConcurrentDesc.Size = new System.Drawing.Size(323, 13);
            this.labelConcurrentDesc.TabIndex = 8;
            this.labelConcurrentDesc.Text = "💡 1: 순차적 다운로드, 3~5: 동시 다운로드 (네트워크 속도 필요)";
            // 
            // numConcurrent
            // 
            this.numConcurrent.Location = new System.Drawing.Point(180, 153);
            this.numConcurrent.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numConcurrent.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numConcurrent.Name = "numConcurrent";
            this.numConcurrent.Size = new System.Drawing.Size(80, 25);
            this.numConcurrent.TabIndex = 7;
            this.numConcurrent.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelConcurrent
            // 
            this.labelConcurrent.AutoSize = true;
            this.labelConcurrent.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.labelConcurrent.Location = new System.Drawing.Point(15, 155);
            this.labelConcurrent.Name = "labelConcurrent";
            this.labelConcurrent.Size = new System.Drawing.Size(120, 19);
            this.labelConcurrent.TabIndex = 6;
            this.labelConcurrent.Text = "동시 다운로드 수:";
            // 
            // labelAudioDesc
            // 
            this.labelAudioDesc.AutoSize = true;
            this.labelAudioDesc.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.labelAudioDesc.ForeColor = System.Drawing.Color.Gray;
            this.labelAudioDesc.Location = new System.Drawing.Point(180, 115);
            this.labelAudioDesc.Name = "labelAudioDesc";
            this.labelAudioDesc.Size = new System.Drawing.Size(258, 13);
            this.labelAudioDesc.TabIndex = 5;
            this.labelAudioDesc.Text = "💡 일반: 128kbps, 고품질: 192kbps, 최고: 320kbps";
            // 
            // numAudio
            // 
            this.numAudio.Location = new System.Drawing.Point(180, 88);
            this.numAudio.Maximum = new decimal(new int[] {
            320,
            0,
            0,
            0});
            this.numAudio.Minimum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this.numAudio.Name = "numAudio";
            this.numAudio.Size = new System.Drawing.Size(80, 25);
            this.numAudio.TabIndex = 4;
            this.numAudio.Value = new decimal(new int[] {
            192,
            0,
            0,
            0});
            // 
            // labelAudio
            // 
            this.labelAudio.AutoSize = true;
            this.labelAudio.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.labelAudio.Location = new System.Drawing.Point(15, 90);
            this.labelAudio.Name = "labelAudio";
            this.labelAudio.Size = new System.Drawing.Size(129, 19);
            this.labelAudio.TabIndex = 3;
            this.labelAudio.Text = "오디오 비트레이트:";
            // 
            // labelGPUDesc
            // 
            this.labelGPUDesc.AutoSize = true;
            this.labelGPUDesc.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.labelGPUDesc.ForeColor = System.Drawing.Color.Gray;
            this.labelGPUDesc.Location = new System.Drawing.Point(180, 48);
            this.labelGPUDesc.Name = "labelGPUDesc";
            this.labelGPUDesc.Size = new System.Drawing.Size(339, 13);
            this.labelGPUDesc.TabIndex = 2;
            this.labelGPUDesc.Text = "💡 CPU는 안정적이고 호환성 높음. GPU는 빠르지만 드라이버 필요.";
            // 
            // comboGPU
            // 
            this.comboGPU.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboGPU.FormattingEnabled = true;
            this.comboGPU.Items.AddRange(new object[] {
            "CPU (기본값)",
            "NVIDIA (CUDA)",
            "AMD (HIP)",
            "Intel (QuickSync)"});
            this.comboGPU.Location = new System.Drawing.Point(180, 22);
            this.comboGPU.Name = "comboGPU";
            this.comboGPU.Size = new System.Drawing.Size(180, 25);
            this.comboGPU.TabIndex = 1;
            // 
            // labelGPU
            // 
            this.labelGPU.AutoSize = true;
            this.labelGPU.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.labelGPU.Location = new System.Drawing.Point(15, 25);
            this.labelGPU.Name = "labelGPU";
            this.labelGPU.Size = new System.Drawing.Size(87, 19);
            this.labelGPU.TabIndex = 0;
            this.labelGPU.Text = "GPU 가속기:";
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(20, 330);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(150, 35);
            this.btnReset.TabIndex = 1;
            this.btnReset.Text = "⟲ 기본값으로 초기화";
            this.btnReset.UseVisualStyleBackColor = false;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // bottomPanel
            // 
            this.bottomPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.bottomPanel.Controls.Add(this.btnExport);
            this.bottomPanel.Controls.Add(this.btnImport);
            this.bottomPanel.Controls.Add(this.button2);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 500);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Padding = new System.Windows.Forms.Padding(20);
            this.bottomPanel.Size = new System.Drawing.Size(600, 70);
            this.bottomPanel.TabIndex = 3;
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Location = new System.Drawing.Point(20, 15);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(140, 38);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "📤 목록 내보내기";
            this.btnExport.UseVisualStyleBackColor = false;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnImport
            // 
            this.btnImport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.btnImport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImport.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnImport.ForeColor = System.Drawing.Color.White;
            this.btnImport.Location = new System.Drawing.Point(170, 15);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(140, 38);
            this.btnImport.TabIndex = 2;
            this.btnImport.Text = "📥 목록 불러오기";
            this.btnImport.UseVisualStyleBackColor = false;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.button2.ForeColor = System.Drawing.Color.White;
            this.button2.Location = new System.Drawing.Point(460, 15);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(120, 38);
            this.button2.TabIndex = 0;
            this.button2.Text = "💾 저장";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            this.button2.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button2.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            // 
            // Setting
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(600, 570);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.headerPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.Name = "Setting";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "설정";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Setting_FormClosing);
            this.Load += new System.EventHandler(this.Setting_Load);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabBasic.ResumeLayout(false);
            this.downloadPathGroup.ResumeLayout(false);
            this.downloadPathGroup.PerformLayout();
            this.downloadFormatGroup.ResumeLayout(false);
            this.downloadFormatGroup.PerformLayout();
            this.formatTypePanel.ResumeLayout(false);
            this.formatTypePanel.PerformLayout();
            this.fileNameGroup.ResumeLayout(false);
            this.fileNameGroup.PerformLayout();
            this.tabAdvanced.ResumeLayout(false);
            this.advancedGroupBox.ResumeLayout(false);
            this.advancedGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numConcurrent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAudio)).EndInit();
            this.bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label headerTitle;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabBasic;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.GroupBox downloadPathGroup;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox downloadFormatGroup;
        private System.Windows.Forms.Panel formatTypePanel;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox fileNameGroup;
        private System.Windows.Forms.Label lblPreviewCaption;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.TextBox txtFileNameTemplate;
        private System.Windows.Forms.Label lblTemplateTitle;
        private System.Windows.Forms.Label lblFileNameDesc;
        private System.Windows.Forms.GroupBox advancedGroupBox;
        private System.Windows.Forms.Label labelConcurrentDesc;
        private System.Windows.Forms.NumericUpDown numConcurrent;
        private System.Windows.Forms.Label labelConcurrent;
        private System.Windows.Forms.Label labelAudioDesc;
        private System.Windows.Forms.NumericUpDown numAudio;
        private System.Windows.Forms.Label labelAudio;
        private System.Windows.Forms.Label labelGPUDesc;
        private System.Windows.Forms.ComboBox comboGPU;
        private System.Windows.Forms.Label labelGPU;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnImport;
    }
}
