using GMTFV.Start;
using GMTFV.tools;
using System;
using System.Windows.Forms;

namespace GMTFV {
    internal static class Program {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Environment.SetEnvironmentVariable(
                "WEBVIEW2_USER_DATA_FOLDER",
                Tol.AppdataPath
            );

            Application.Run(new MainForm());
        }
    }
}
