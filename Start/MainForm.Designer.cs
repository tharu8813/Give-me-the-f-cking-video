using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.Start {
    partial class MainFrom {
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();

            this.label1 = new Label();
            this.progressBar1 = new ProgressBar();
            this.button1 = new Button();
            this.label3 = new Label();
            this.label8 = new Label();
            this.panel1 = new Panel();
            this.panel2 = new Panel();
            this.dataGridView1 = new DataGridView();
            this.object_save = new DataGridViewTextBoxColumn();
            this.Select = new DataGridViewCheckBoxColumn();
            this.Index = new DataGridViewTextBoxColumn();
            this.Image = new DataGridViewImageColumn();
            this.TItle = new DataGridViewTextBoxColumn();
            this.By = new DataGridViewTextBoxColumn();
            this.VideoTIme = new DataGridViewTextBoxColumn();
            this.Upload = new DataGridViewTextBoxColumn();
            this.Info = new DataGridViewButtonColumn();
            this.panel3 = new Panel();
            this.button4 = new Button();
            this.button3 = new Button();
            this.button2 = new Button();
            this.toolTip1 = new ToolTip(this.components);

            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.panel3.SuspendLayout();
            this.SuspendLayout();

            // 
            // label1
            // 
            this.label1.Dock = DockStyle.Top;
            this.label1.Font = new Font("맑은 고딕", 21.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new Point(10, 10);
            this.label1.Name = "label1";
            this.label1.Size = new Size(791, 40);
            this.label1.TabIndex = 0;
            this.label1.Text = "야발 동영상 내놔";

            // 
            // progressBar1
            // 
            this.progressBar1.Dock = DockStyle.Bottom;
            this.progressBar1.Location = new Point(10, 412);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new Size(791, 23);
            this.progressBar1.TabIndex = 13;

            // 
            // button1
            // 
            this.button1.Dock = DockStyle.Right;
            this.button1.Location = new Point(743, 0);
            this.button1.Name = "button1";
            this.button1.Size = new Size(48, 23);
            this.button1.TabIndex = 16;
            this.button1.Text = "설정";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new EventHandler(this.button1_Click);

            // 
            // label3
            // 
            this.label3.Dock = DockStyle.Top;
            this.label3.Location = new Point(10, 50);
            this.label3.Name = "label3";
            this.label3.Size = new Size(791, 15);
            this.label3.TabIndex = 17;
            this.label3.Text = "  유튜브 동영상을 보다 쉽게 다운받으세요!";

            // 
            // label8
            // 
            this.label8.Dock = DockStyle.Bottom;
            this.label8.Location = new Point(10, 393);
            this.label8.Name = "label8";
            this.label8.Size = new Size(791, 19);
            this.label8.TabIndex = 21;
            this.label8.Text = "?% (0/0)";
            this.label8.TextAlign = ContentAlignment.MiddleRight;

            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = DockStyle.Top;
            this.panel1.Location = new Point(10, 65);
            this.panel1.Name = "panel1";
            this.panel1.Size = new Size(791, 23);
            this.panel1.TabIndex = 22;

            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dataGridView1);
            this.panel2.Dock = DockStyle.Fill;
            this.panel2.Location = new Point(10, 88);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new Padding(5);
            this.panel2.Size = new Size(791, 305);
            this.panel2.TabIndex = 23;
            this.panel2.Paint += new PaintEventHandler(this.panel2_Paint);

            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowDrop = true;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.NullValue = "NULL";
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new DataGridViewColumn[] {
                this.object_save,
                this.Select,
                this.Index,
                this.Image,
                this.TItle,
                this.By,
                this.VideoTIme,
                this.Upload,
                this.Info});
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.Dock = DockStyle.Fill;
            this.dataGridView1.Location = new Point(5, 5);
            this.dataGridView1.Margin = new Padding(0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 30;
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new Size(781, 295);
            this.dataGridView1.TabIndex = 24;
            this.dataGridView1.CellContentClick += new DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.DragDrop += new DragEventHandler(this.dataGridView1_DragDrop);
            this.dataGridView1.DragEnter += new DragEventHandler(this.dataGridView1_DragEnter);
            this.dataGridView1.DragLeave += new EventHandler(this.dataGridView1_DragLeave);
            this.dataGridView1.KeyDown += new KeyEventHandler(this.dataGridView1_KeyDown);

            // 
            // object_save
            // 
            this.object_save.HeaderText = "객체";
            this.object_save.Name = "object_save";
            this.object_save.ReadOnly = true;
            this.object_save.Visible = false;

            // 
            // Select
            // 
            this.Select.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            this.Select.FillWeight = 30F;
            this.Select.HeaderText = "";
            this.Select.MinimumWidth = 30;
            this.Select.Name = "Select";
            this.Select.Width = 30;

            // 
            // Index
            // 
            this.Index.FillWeight = 30F;
            this.Index.HeaderText = "No";
            this.Index.Name = "Index";
            this.Index.ReadOnly = true;
            this.Index.Width = 30;

            // 
            // Image
            // 
            this.Image.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Image.FillWeight = 50F;
            this.Image.HeaderText = "썸네일";
            this.Image.ImageLayout = DataGridViewImageCellLayout.Zoom;
            this.Image.Name = "Image";
            this.Image.ReadOnly = true;

            // 
            // TItle
            // 
            this.TItle.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.TItle.HeaderText = "제목";
            this.TItle.Name = "TItle";
            this.TItle.ReadOnly = true;

            // 
            // By
            // 
            this.By.HeaderText = "저자";
            this.By.Name = "By";
            this.By.ReadOnly = true;

            // 
            // VideoTIme
            // 
            this.VideoTIme.HeaderText = "길이";
            this.VideoTIme.Name = "VideoTIme";
            this.VideoTIme.ReadOnly = true;

            // 
            // Upload
            // 
            this.Upload.HeaderText = "업로드 일자";
            this.Upload.Name = "Upload";
            this.Upload.ReadOnly = true;

            // 
            // Info
            // 
            this.Info.FillWeight = 40F;
            this.Info.HeaderText = "상세";
            this.Info.Name = "Info";
            this.Info.ReadOnly = true;
            this.Info.Width = 40;

            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.button4);
            this.panel3.Controls.Add(this.button3);
            this.panel3.Controls.Add(this.button2);
            this.panel3.Dock = DockStyle.Bottom;
            this.panel3.Location = new Point(10, 435);
            this.panel3.Name = "panel3";
            this.panel3.Size = new Size(791, 33);
            this.panel3.TabIndex = 24;

            // 
            // button4
            // 
            this.button4.Dock = DockStyle.Fill;
            this.button4.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(129)));
            this.button4.Location = new Point(0, 0);
            this.button4.Name = "button4";
            this.button4.Size = new Size(647, 33);
            this.button4.TabIndex = 13;
            this.button4.Text = "다운로드하기";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new EventHandler(this.button4_Click);

            // 
            // button3
            // 
            this.button3.Dock = DockStyle.Right;
            this.button3.Location = new Point(647, 0);
            this.button3.Name = "button3";
            this.button3.Size = new Size(75, 33);
            this.button3.TabIndex = 26;
            this.button3.Text = "삭제하기";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new EventHandler(this.button3_Click);

            // 
            // button2
            // 
            this.button2.Dock = DockStyle.Right;
            this.button2.Location = new Point(722, 0);
            this.button2.Name = "button2";
            this.button2.Size = new Size(69, 33);
            this.button2.TabIndex = 25;
            this.button2.Text = "추가하기";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new EventHandler(this.button2_Click);

            // 
            // toolTip1
            // 
            this.toolTip1.AutomaticDelay = 0;
            this.toolTip1.IsBalloon = true;
            this.toolTip1.ToolTipTitle = "상세 정보";

            // 
            // MainFrom
            // 
            this.ClientSize = new Size(811, 478);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel3);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximumSize = new Size(1000, 750);
            this.MinimumSize = new Size(600, 300);
            this.Name = "MainFrom";
            this.Padding = new Padding(10);
            this.Text = "야발 동영상 내놔";
            this.Load += new EventHandler(this.MainFrom_Load);

            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((ISupportInitialize)(this.dataGridView1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private ProgressBar progressBar1;
        private Button button1;
        private Label label3;
        private Label label8;
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
    }
}