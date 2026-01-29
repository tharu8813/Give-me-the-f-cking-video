using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace GMTFV.Start {
    partial class VideoCheckcs {
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
            this.webView21 = new WebView2();

            ((ISupportInitialize)(this.webView21)).BeginInit();
            this.SuspendLayout();

            // 
            // webView21
            // 
            this.webView21.AllowExternalDrop = true;
            this.webView21.CreationProperties = null;
            this.webView21.DefaultBackgroundColor = Color.White;
            this.webView21.Dock = DockStyle.Fill;
            this.webView21.Location = new Point(0, 0);
            this.webView21.Name = "webView21";
            this.webView21.Size = new Size(800, 450);
            this.webView21.TabIndex = 0;
            this.webView21.ZoomFactor = 1D;

            // 
            // VideoCheckcs
            // 
            this.ClientSize = new Size(800, 450);
            this.Controls.Add(this.webView21);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox = false;
            this.Name = "VideoCheckcs";
            this.Text = "비디오";
            this.FormClosing += new FormClosingEventHandler(this.VideoCheckcs_FormClosing);
            this.Load += new EventHandler(this.VideoCheckcs_Load);

            ((ISupportInitialize)(this.webView21)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private WebView2 webView21;
    }
}