using System.Drawing;
using System.Windows.Forms;

namespace GMTFV.services {
    /// <summary>WinForms 보조 창에 동일한 색상·여백·버튼 위계를 적용합니다.</summary>
    internal static class FormTheme {
        public static readonly Color Surface = Color.FromArgb(248, 250, 252);
        public static readonly Color Card = Color.White;
        public static readonly Color Header = Color.FromArgb(15, 23, 42);
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color Secondary = Color.FromArgb(51, 65, 85);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color Text = Color.FromArgb(15, 23, 42);
        public static readonly Color MutedText = Color.FromArgb(100, 116, 139);

        public static void Apply(Form form, Panel headerPanel = null) {
            form.BackColor = Surface;
            form.Font = new Font("맑은 고딕", 9F);
            form.ForeColor = Text;
            if (headerPanel != null) {
                headerPanel.BackColor = Header;
                headerPanel.Padding = new Padding(24, 16, 24, 16);
                foreach (Control control in headerPanel.Controls) {
                    if (control is Label label) {
                        label.ForeColor = Color.White;
                        if (label.Font.Size >= 14F) label.Font = new Font("맑은 고딕", 18F, FontStyle.Bold);
                    }
                }
            }
            StyleControls(form.Controls, headerPanel);
        }

        public static void PrimaryButton(Button button) => StyleButton(button, Primary, Color.White, 0);
        public static void SecondaryButton(Button button) => StyleButton(button, Secondary, Color.White, 0);
        public static void DangerButton(Button button) => StyleButton(button, Danger, Color.White, 0);

        public static void OutlineButton(Button button) {
            StyleButton(button, Color.White, Secondary, 1);
            button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        }

        private static void StyleControls(Control.ControlCollection controls, Control headerPanel) {
            foreach (Control control in controls) {
                if (control is Panel panel && panel != headerPanel) {
                    panel.BackColor = panel.Name.IndexOf("bottom", System.StringComparison.OrdinalIgnoreCase) >= 0 ? Surface : Card;
                }
                else if (control is GroupBox groupBox) {
                    groupBox.BackColor = Card;
                    groupBox.ForeColor = Text;
                    groupBox.Padding = new Padding(16, 28, 16, 16);
                    groupBox.FlatStyle = FlatStyle.Flat;
                } else if (control is TextBox textBox) {
                    textBox.BackColor = Card;
                    textBox.ForeColor = Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                } else if (control is ComboBox comboBox) {
                    comboBox.BackColor = Card;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                } else if (control is NumericUpDown numeric) {
                    numeric.BackColor = Card;
                    numeric.ForeColor = Text;
                    numeric.BorderStyle = BorderStyle.FixedSingle;
                } else if (control is TabControl tabControl) {
                    tabControl.Padding = new Point(16, 6);
                } else if (control is TabPage tabPage) {
                    tabPage.BackColor = Card;
                } else if (control is Label label && control.Parent != headerPanel) {
                    label.ForeColor = label.Font.Size <= 8.5F ? MutedText : Text;
                }
                if (control.HasChildren) StyleControls(control.Controls, headerPanel);
            }
        }

        private static void StyleButton(Button button, Color background, Color foreground, int borderSize) {
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = borderSize;
            button.Cursor = Cursors.Hand;
            button.Height = System.Math.Max(button.Height, 34);
            button.Padding = new Padding(8, 0, 8, 0);
        }
    }
}
