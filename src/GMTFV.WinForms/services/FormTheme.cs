using System.Drawing;

namespace GMTFV.services {
    /// <summary>WinForms 보조 창에 동일한 색상를 적용합니다.</summary>
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
    }
}
