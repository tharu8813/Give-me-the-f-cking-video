using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    partial class MainForm {
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lblToolVersions = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dragDropLabel = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.object_save = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Select = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Index = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Image = new System.Windows.Forms.DataGridViewImageColumn();
            this.TItle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.By = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.VideoTIme = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Upload = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Info = new System.Windows.Forms.DataGridViewButtonColumn();
            this.panel3 = new System.Windows.Forms.Panel();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.headerPanel = new System.Windows.Forms.Panel();
            this.chromeTabsButton = new System.Windows.Forms.Button();
            this.emptyStateLabel = new System.Windows.Forms.Label();
            this.concurrentProgressPanel = new System.Windows.Forms.TableLayoutPanel();
            this.progressSlot1 = new System.Windows.Forms.Panel();
            this.dynamicProgressBar1 = new System.Windows.Forms.ProgressBar();
            this.dynamicStatusLabel1 = new System.Windows.Forms.Label();
            this.progressSlot2 = new System.Windows.Forms.Panel();
            this.dynamicProgressBar2 = new System.Windows.Forms.ProgressBar();
            this.dynamicStatusLabel2 = new System.Windows.Forms.Label();
            this.progressSlot3 = new System.Windows.Forms.Panel();
            this.dynamicProgressBar3 = new System.Windows.Forms.ProgressBar();
            this.dynamicStatusLabel3 = new System.Windows.Forms.Label();
            this.progressSlot4 = new System.Windows.Forms.Panel();
            this.dynamicProgressBar4 = new System.Windows.Forms.ProgressBar();
            this.dynamicStatusLabel4 = new System.Windows.Forms.Label();
            this.progressSlot5 = new System.Windows.Forms.Panel();
            this.dynamicProgressBar5 = new System.Windows.Forms.ProgressBar();
            this.dynamicStatusLabel5 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.panel3.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.concurrentProgressPanel.SuspendLayout();
            this.progressSlot1.SuspendLayout();
            this.progressSlot2.SuspendLayout();
            this.progressSlot3.SuspendLayout();
            this.progressSlot4.SuspendLayout();
            this.progressSlot5.SuspendLayout();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("맑은 고딕", 24F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(20, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 45);
            this.label1.TabIndex = 0;
            this.label1.Text = "GMTFV";
            //
            // progressBar1
            //
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar1.Location = new System.Drawing.Point(20, 10);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(960, 25);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 0;
            //
            // button1
            //
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.Dock = System.Windows.Forms.DockStyle.Right;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold);
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(880, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 30);
            this.button1.TabIndex = 0;
            this.button1.Text = "⚙️ 설정";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            this.button1.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button1.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.label3.Location = new System.Drawing.Point(24, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(307, 19);
            this.label3.TabIndex = 1;
            this.label3.Text = "YouTube 영상과 오디오를 깔끔하게 저장하세요";
            //
            // label8
            //
            this.label8.Dock = System.Windows.Forms.DockStyle.Top;
            this.label8.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.label8.Location = new System.Drawing.Point(20, 35);
            this.label8.Name = "label8";
            this.label8.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
            this.label8.Size = new System.Drawing.Size(960, 41);
            this.label8.TabIndex = 1;
            this.label8.Text = "준비됨 · URL을 추가해 다운로드 목록을 만들어 보세요";
            //
            // lblToolVersions
            //
            this.lblToolVersions.AutoSize = true;
            this.lblToolVersions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblToolVersions.Font = new System.Drawing.Font("맑은 고딕", 8.25F);
            this.lblToolVersions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.lblToolVersions.Location = new System.Drawing.Point(20, 107);
            this.lblToolVersions.Name = "lblToolVersions";
            this.lblToolVersions.Size = new System.Drawing.Size(195, 13);
            this.lblToolVersions.TabIndex = 5;
            this.lblToolVersions.Text = "yt-dlp: 확인 중...  |  FFmpeg: 확인 중...";
            this.lblToolVersions.TextAlign = System.Drawing.ContentAlignment.TopRight;
            //
            // panel1
            //
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.chromeTabsButton);
            this.panel1.Controls.Add(this.dragDropLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 100);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.panel1.Size = new System.Drawing.Size(1000, 50);
            this.panel1.TabIndex = 1;
            //
            // dragDropLabel
            //
            this.dragDropLabel.AutoSize = true;
            this.dragDropLabel.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.dragDropLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.dragDropLabel.Location = new System.Drawing.Point(20, 16);
            this.dragDropLabel.Name = "dragDropLabel";
            this.dragDropLabel.Size = new System.Drawing.Size(384, 15);
            this.dragDropLabel.TabIndex = 1;
            this.dragDropLabel.Text = "URL·목록 파일을 끌어 놓거나, Chrome 탭을 한 번에 가져올 수 있어요.";
            //
            // chromeTabsButton
            //
            this.chromeTabsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(56)))), ((int)(((byte)(202)))));
            this.chromeTabsButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chromeTabsButton.Dock = System.Windows.Forms.DockStyle.Right;
            this.chromeTabsButton.FlatAppearance.BorderSize = 0;
            this.chromeTabsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chromeTabsButton.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.chromeTabsButton.ForeColor = System.Drawing.Color.White;
            this.chromeTabsButton.Name = "chromeTabsButton";
            this.chromeTabsButton.Size = new System.Drawing.Size(130, 30);
            this.chromeTabsButton.TabIndex = 2;
            this.chromeTabsButton.Text = "◉ Chrome 탭";
            this.toolTip1.SetToolTip(this.chromeTabsButton, "열린 YouTube 탭을 한 번에 추가합니다");
            this.chromeTabsButton.UseVisualStyleBackColor = false;
            this.chromeTabsButton.Click += new System.EventHandler(this.ChromeTabsButton_Click);
            this.chromeTabsButton.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.chromeTabsButton.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            //
            // panel2
            //
            this.panel2.BackColor = System.Drawing.Color.White;
            this.panel2.Controls.Add(this.emptyStateLabel);
            this.panel2.Controls.Add(this.dataGridView1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 150);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.panel2.Size = new System.Drawing.Size(1000, 427);
            this.panel2.TabIndex = 2;
            this.panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            //
            // emptyStateLabel
            //
            this.emptyStateLabel.AllowDrop = true;
            this.emptyStateLabel.BackColor = System.Drawing.Color.White;
            this.emptyStateLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.emptyStateLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.emptyStateLabel.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this.emptyStateLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.emptyStateLabel.Name = "emptyStateLabel";
            this.emptyStateLabel.Size = new System.Drawing.Size(420, 116);
            this.emptyStateLabel.TabIndex = 1;
            this.emptyStateLabel.Text = "아직 다운로드 목록이 비어 있습니다\r\n\r\nURL 추가 · Chrome 탭 가져오기 · 드래그 앤 드롭 중 편한 방법을 선택하세요.";
            this.emptyStateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.emptyStateLabel.Click += new System.EventHandler(this.emptyStateLabel_Click);
            this.emptyStateLabel.DragDrop += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragDrop);
            this.emptyStateLabel.DragEnter += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragEnter);
            this.emptyStateLabel.DragLeave += new System.EventHandler(this.dataGridView1_DragLeave);
            //
            // dataGridView1
            //
            this.dataGridView1.AllowDrop = true;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.ColumnHeadersHeight = 40;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.object_save,
            this.Select,
            this.Index,
            this.Image,
            this.TItle,
            this.By,
            this.VideoTIme,
            this.Upload,
            this.Info});
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(62)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(5);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(234)))), ((int)(((byte)(254)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.EnableHeadersVisualStyles = false;
            this.dataGridView1.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.dataGridView1.Location = new System.Drawing.Point(20, 15);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 30;
            dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(5);
            this.dataGridView1.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dataGridView1.RowTemplate.Height = 84;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(960, 397);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragDrop);
            this.dataGridView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragEnter);
            this.dataGridView1.DragLeave += new System.EventHandler(this.dataGridView1_DragLeave);
            this.dataGridView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridView1_KeyDown);
            //
            // object_save
            //
            this.object_save.HeaderText = "객체";
            this.object_save.MinimumWidth = 8;
            this.object_save.Name = "object_save";
            this.object_save.ReadOnly = true;
            this.object_save.Visible = false;
            this.object_save.Width = 150;
            //
            // Select
            //
            this.Select.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Select.FillWeight = 40F;
            this.Select.HeaderText = "선택";
            this.Select.MinimumWidth = 40;
            this.Select.Name = "Select";
            this.Select.Width = 40;
            //
            // Index
            //
            this.Index.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Index.FillWeight = 50F;
            this.Index.HeaderText = "No";
            this.Index.MinimumWidth = 8;
            this.Index.Name = "Index";
            this.Index.ReadOnly = true;
            this.Index.Width = 50;
            //
            // Image
            //
            this.Image.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Image.FillWeight = 120F;
            this.Image.HeaderText = "썸네일";
            this.Image.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.Image.MinimumWidth = 8;
            this.Image.Name = "Image";
            this.Image.ReadOnly = true;
            this.Image.Width = 120;
            //
            // TItle
            //
            this.TItle.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.TItle.HeaderText = "제목";
            this.TItle.MinimumWidth = 8;
            this.TItle.Name = "TItle";
            this.TItle.ReadOnly = true;
            //
            // By
            //
            this.By.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.By.HeaderText = "채널";
            this.By.MinimumWidth = 8;
            this.By.Name = "By";
            this.By.ReadOnly = true;
            this.By.Width = 150;
            //
            // VideoTIme
            //
            this.VideoTIme.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.VideoTIme.HeaderText = "길이";
            this.VideoTIme.MinimumWidth = 8;
            this.VideoTIme.Name = "VideoTIme";
            this.VideoTIme.ReadOnly = true;
            this.VideoTIme.Width = 80;
            //
            // Upload
            //
            this.Upload.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Upload.HeaderText = "업로드일";
            this.Upload.MinimumWidth = 8;
            this.Upload.Name = "Upload";
            this.Upload.ReadOnly = true;
            this.Upload.Width = 110;
            //
            // Info
            //
            this.Info.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Info.FillWeight = 70F;
            this.Info.HeaderText = "상세";
            this.Info.MinimumWidth = 8;
            this.Info.Name = "Info";
            this.Info.ReadOnly = true;
            this.Info.Width = 70;
            //
            // panel3
            //
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.panel3.Controls.Add(this.concurrentProgressPanel);
            this.panel3.Controls.Add(this.button4);
            this.panel3.Controls.Add(this.button3);
            this.panel3.Controls.Add(this.button2);
            this.panel3.Controls.Add(this.lblToolVersions);
            this.panel3.Controls.Add(this.label8);
            this.panel3.Controls.Add(this.progressBar1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 577);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(20, 10, 20, 20);
            this.panel3.Size = new System.Drawing.Size(1000, 140);
            this.panel3.TabIndex = 3;
            //
            // concurrentProgressPanel
            //
            this.concurrentProgressPanel.ColumnCount = 1;
            this.concurrentProgressPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.concurrentProgressPanel.Controls.Add(this.progressSlot1, 0, 0);
            this.concurrentProgressPanel.Controls.Add(this.progressSlot2, 0, 1);
            this.concurrentProgressPanel.Controls.Add(this.progressSlot3, 0, 2);
            this.concurrentProgressPanel.Controls.Add(this.progressSlot4, 0, 3);
            this.concurrentProgressPanel.Controls.Add(this.progressSlot5, 0, 4);
            this.concurrentProgressPanel.Location = new System.Drawing.Point(20, 76);
            this.concurrentProgressPanel.Name = "concurrentProgressPanel";
            this.concurrentProgressPanel.RowCount = 5;
            this.concurrentProgressPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.concurrentProgressPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.concurrentProgressPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.concurrentProgressPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.concurrentProgressPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.concurrentProgressPanel.Size = new System.Drawing.Size(960, 320);
            this.concurrentProgressPanel.TabIndex = 6;
            this.concurrentProgressPanel.Visible = false;
            //
            // progressSlot1
            //
            this.progressSlot1.Controls.Add(this.dynamicStatusLabel1);
            this.progressSlot1.Controls.Add(this.dynamicProgressBar1);
            this.progressSlot1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressSlot1.Name = "progressSlot1";
            this.progressSlot1.TabIndex = 0;
            //
            // dynamicProgressBar1
            //
            this.dynamicProgressBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicProgressBar1.Maximum = 100;
            this.dynamicProgressBar1.Name = "dynamicProgressBar1";
            this.dynamicProgressBar1.Size = new System.Drawing.Size(960, 18);
            this.dynamicProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            //
            // dynamicStatusLabel1
            //
            this.dynamicStatusLabel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicStatusLabel1.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.dynamicStatusLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.dynamicStatusLabel1.Name = "dynamicStatusLabel1";
            this.dynamicStatusLabel1.Size = new System.Drawing.Size(960, 40);
            this.dynamicStatusLabel1.Text = "대기 중...";
            //
            // progressSlot2
            //
            this.progressSlot2.Controls.Add(this.dynamicStatusLabel2);
            this.progressSlot2.Controls.Add(this.dynamicProgressBar2);
            this.progressSlot2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressSlot2.Name = "progressSlot2";
            this.progressSlot2.TabIndex = 1;
            //
            // dynamicProgressBar2
            //
            this.dynamicProgressBar2.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicProgressBar2.Maximum = 100;
            this.dynamicProgressBar2.Name = "dynamicProgressBar2";
            this.dynamicProgressBar2.Size = new System.Drawing.Size(960, 18);
            this.dynamicProgressBar2.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            //
            // dynamicStatusLabel2
            //
            this.dynamicStatusLabel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicStatusLabel2.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.dynamicStatusLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.dynamicStatusLabel2.Name = "dynamicStatusLabel2";
            this.dynamicStatusLabel2.Size = new System.Drawing.Size(960, 40);
            this.dynamicStatusLabel2.Text = "대기 중...";
            //
            // progressSlot3
            //
            this.progressSlot3.Controls.Add(this.dynamicStatusLabel3);
            this.progressSlot3.Controls.Add(this.dynamicProgressBar3);
            this.progressSlot3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressSlot3.Name = "progressSlot3";
            this.progressSlot3.TabIndex = 2;
            //
            // dynamicProgressBar3
            //
            this.dynamicProgressBar3.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicProgressBar3.Maximum = 100;
            this.dynamicProgressBar3.Name = "dynamicProgressBar3";
            this.dynamicProgressBar3.Size = new System.Drawing.Size(960, 18);
            this.dynamicProgressBar3.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            //
            // dynamicStatusLabel3
            //
            this.dynamicStatusLabel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicStatusLabel3.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.dynamicStatusLabel3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.dynamicStatusLabel3.Name = "dynamicStatusLabel3";
            this.dynamicStatusLabel3.Size = new System.Drawing.Size(960, 40);
            this.dynamicStatusLabel3.Text = "대기 중...";
            //
            // progressSlot4
            //
            this.progressSlot4.Controls.Add(this.dynamicStatusLabel4);
            this.progressSlot4.Controls.Add(this.dynamicProgressBar4);
            this.progressSlot4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressSlot4.Name = "progressSlot4";
            this.progressSlot4.TabIndex = 3;
            //
            // dynamicProgressBar4
            //
            this.dynamicProgressBar4.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicProgressBar4.Maximum = 100;
            this.dynamicProgressBar4.Name = "dynamicProgressBar4";
            this.dynamicProgressBar4.Size = new System.Drawing.Size(960, 18);
            this.dynamicProgressBar4.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            //
            // dynamicStatusLabel4
            //
            this.dynamicStatusLabel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicStatusLabel4.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.dynamicStatusLabel4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.dynamicStatusLabel4.Name = "dynamicStatusLabel4";
            this.dynamicStatusLabel4.Size = new System.Drawing.Size(960, 40);
            this.dynamicStatusLabel4.Text = "대기 중...";
            //
            // progressSlot5
            //
            this.progressSlot5.Controls.Add(this.dynamicStatusLabel5);
            this.progressSlot5.Controls.Add(this.dynamicProgressBar5);
            this.progressSlot5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressSlot5.Name = "progressSlot5";
            this.progressSlot5.TabIndex = 4;
            //
            // dynamicProgressBar5
            //
            this.dynamicProgressBar5.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicProgressBar5.Maximum = 100;
            this.dynamicProgressBar5.Name = "dynamicProgressBar5";
            this.dynamicProgressBar5.Size = new System.Drawing.Size(960, 18);
            this.dynamicProgressBar5.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            //
            // dynamicStatusLabel5
            //
            this.dynamicStatusLabel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.dynamicStatusLabel5.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.dynamicStatusLabel5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(127)))), ((int)(((byte)(140)))), ((int)(((byte)(141)))));
            this.dynamicStatusLabel5.Name = "dynamicStatusLabel5";
            this.dynamicStatusLabel5.Size = new System.Drawing.Size(960, 40);
            this.dynamicStatusLabel5.Text = "대기 중...";
            //
            // button4
            //
            this.button4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(118)))), ((int)(((byte)(110)))));
            this.button4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button4.FlatAppearance.BorderSize = 0;
            this.button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button4.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.button4.ForeColor = System.Drawing.Color.White;
            this.button4.Location = new System.Drawing.Point(20, 76);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(660, 31);
            this.button4.TabIndex = 4;
            this.button4.Text = "⬇️ 다운로드 시작";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            this.button4.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button4.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            //
            // button3
            //
            this.button3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.button3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button3.Dock = System.Windows.Forms.DockStyle.Right;
            this.button3.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(202)))), ((int)(((byte)(202)))));
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.button3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.button3.Location = new System.Drawing.Point(680, 76);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(150, 31);
            this.button3.TabIndex = 3;
            this.button3.Text = "🗑️ 선택 삭제";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            this.button3.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button3.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            //
            // button2
            //
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.Dock = System.Windows.Forms.DockStyle.Right;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.button2.ForeColor = System.Drawing.Color.White;
            this.button2.Location = new System.Drawing.Point(830, 76);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(150, 31);
            this.button2.TabIndex = 2;
            this.button2.Text = "➕ URL 추가";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            this.button2.MouseEnter += new System.EventHandler(this.Button_MouseEnter);
            this.button2.MouseLeave += new System.EventHandler(this.Button_MouseLeave);
            //
            // toolTip1
            //
            this.toolTip1.IsBalloon = true;
            this.toolTip1.ToolTipTitle = "정보";
            //
            // headerPanel
            //
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.headerPanel.Controls.Add(this.label1);
            this.headerPanel.Controls.Add(this.label3);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Padding = new System.Windows.Forms.Padding(20, 15, 20, 15);
            this.headerPanel.Size = new System.Drawing.Size(1000, 100);
            this.headerPanel.TabIndex = 0;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1000, 717);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.panel3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximumSize = new System.Drawing.Size(1920, 1080);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.Text = "GMTFV - 유튜브 동영상 다운로더";
            this.Load += new System.EventHandler(this.MainFrom_Load);
            this.Shown += new System.EventHandler(this.MainFrom_Shown);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.concurrentProgressPanel.ResumeLayout(false);
            this.progressSlot1.ResumeLayout(false);
            this.progressSlot2.ResumeLayout(false);
            this.progressSlot3.ResumeLayout(false);
            this.progressSlot4.ResumeLayout(false);
            this.progressSlot5.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Label label1;
        private ProgressBar progressBar1;
        private Button button1;
        private Label label3;
        private Label label8;
        private Label lblToolVersions;
        private Panel panel1;
        private Panel panel2;
        private DataGridView dataGridView1;
        private Panel panel3;
        private Button button4;
        private Button button2;
        private ToolTip toolTip1;
        private Button button3;
        private DataGridViewTextBoxColumn object_save;
        private new DataGridViewCheckBoxColumn Select;
        private DataGridViewTextBoxColumn Index;
        private DataGridViewImageColumn Image;
        private DataGridViewTextBoxColumn TItle;
        private DataGridViewTextBoxColumn By;
        private DataGridViewTextBoxColumn VideoTIme;
        private DataGridViewTextBoxColumn Upload;
        private DataGridViewButtonColumn Info;
        private Panel headerPanel;
        private Label dragDropLabel;
        private Button chromeTabsButton;
        private Label emptyStateLabel;
        private TableLayoutPanel concurrentProgressPanel;
        private Panel progressSlot1;
        private Panel progressSlot2;
        private Panel progressSlot3;
        private Panel progressSlot4;
        private Panel progressSlot5;
        private ProgressBar dynamicProgressBar1;
        private ProgressBar dynamicProgressBar2;
        private ProgressBar dynamicProgressBar3;
        private ProgressBar dynamicProgressBar4;
        private ProgressBar dynamicProgressBar5;
        private Label dynamicStatusLabel1;
        private Label dynamicStatusLabel2;
        private Label dynamicStatusLabel3;
        private Label dynamicStatusLabel4;
        private Label dynamicStatusLabel5;
    }
}
