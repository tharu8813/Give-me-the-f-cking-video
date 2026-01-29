using GMTFV.Properties;
using GMTFV.tools;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace GMTFV.Start {
    public partial class Setting : DevForm {
        private CommonOpenFileDialog commonOpenFileDialog;

        public Setting() {
            InitializeComponent();
        }

        private void Setting_Load(object sender, EventArgs e) {
            commonOpenFileDialog = new CommonOpenFileDialog();
            commonOpenFileDialog.IsFolderPicker = true;

            textBox2.Text = Settings.Default.Path;

            if (Settings.Default.IsTypeVideo) {
                radioButton1.Checked = true;
            } else {
                radioButton2.Checked = true;
            }

            UpdateList();
            comboBox1.SelectedItem = Settings.Default.SubType;
        }

        private void UpdateList() {
            comboBox1.Items.Clear();

            string[] formats = radioButton1.Checked ? Tol.VideoFormats : Tol.AudioFormats;
            comboBox1.Items.AddRange(formats);

            string subType = Settings.Default.SubType;
            comboBox1.SelectedItem = comboBox1.Items.Contains(subType) ? subType : comboBox1.Items[0];
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("https://www.microsoft.com/store/productId/9MVZQVXJBQ9V?ocid=pdpshare");
        }

        private void button3_Click(object sender, EventArgs e) {
            commonOpenFileDialog.InitialDirectory = Settings.Default.Path;

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok) {
                textBox2.Text = commonOpenFileDialog.FileName;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            UpdateList();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) {
            UpdateList();
        }

        private void button2_Click(object sender, EventArgs e) {
            SaveSetting();
        }

        private void SaveSetting() {
            Settings.Default.SubType = comboBox1.SelectedItem?.ToString() ?? "";
            Settings.Default.Path = textBox2.Text;
            Settings.Default.IsTypeVideo = radioButton1.Checked;
            Settings.Default.Save();
            Dispose();
        }

        private void Setting_FormClosing(object sender, FormClosingEventArgs e) {
            if ((comboBox1.SelectedItem.ToString() != Settings.Default.SubType ||
                 textBox2.Text != Settings.Default.Path ||
                 radioButton1.Checked != Settings.Default.IsTypeVideo) &&
                Tol.ShowQ("변경사항이 있습니다. 저장하시겠습니까?")) {
                SaveSetting();
            }
        }
    }
}