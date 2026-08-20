using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GMTFV.Wpf.Services;

/// <summary>Windows 제목 표시줄을 앱의 다크 팔레트와 일치시킵니다.</summary>
internal static class DarkModeWindowHelper {
    private const int ImmersiveDarkModeBefore20H1 = 19;
    private const int ImmersiveDarkMode = 20;

    public static void Apply(Window window) {
        ArgumentNullException.ThrowIfNull(window);
        window.SourceInitialized += (_, _) => {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            int enabled = 1;
            if (DwmSetWindowAttribute(handle, ImmersiveDarkMode, ref enabled, sizeof(int)) != 0) {
                _ = DwmSetWindowAttribute(handle, ImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
            }
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);
}
