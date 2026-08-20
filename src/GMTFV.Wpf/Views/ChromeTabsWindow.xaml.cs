using GMTFV.Wpf.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class ChromeTabsWindow : Window {
    private readonly ChromeTabImportService service;
    private readonly bool serviceAvailable;

    public ChromeTabsWindow(ChromeTabImportService service, bool serviceAvailable) {
        this.service = service;
        this.serviceAvailable = serviceAvailable;
        InitializeComponent();
        DarkModeWindowHelper.Apply(this);
        service.TabsReceived += Service_TabsReceived;
        Closed += (_, _) => service.TabsReceived -= Service_TabsReceived;
        if (!serviceAvailable) RequestStatus.Text = "통신 포트(43128)를 사용할 수 없습니다. 다른 GMTFV 창을 종료한 뒤 다시 실행해주세요.";
    }

    private void Current_Click(object sender, RoutedEventArgs e) => Request(ChromeTabRequestMode.Current, "현재 활성 탭");
    private void Selected_Click(object sender, RoutedEventArgs e) => Request(ChromeTabRequestMode.Selected, "선택한 탭");
    private void All_Click(object sender, RoutedEventArgs e) => Request(ChromeTabRequestMode.All, "전체 YouTube 탭");

    private void Request(ChromeTabRequestMode mode, string label) {
        if (!serviceAvailable) { RequestStatus.Text = "Chrome 확장과 통신할 수 없습니다."; return; }
        service.RequestTabs(mode);
        RequestStatus.Text = $"Chrome 확장에 {label}을 요청했습니다. 확장이 응답하면 자동으로 대기열에 추가됩니다.";
    }

    private void Service_TabsReceived(object? sender, IReadOnlyList<string> urls) => Dispatcher.BeginInvoke(() =>
        RequestStatus.Text = urls.Count == 0
            ? "확장이 응답했지만 선택한 범위에서 YouTube 탭을 찾지 못했습니다."
            : $"응답 완료 · YouTube 탭 {urls.Count}개를 대기열에 전달했습니다.");

    private void InstallHelp_Click(object sender, RoutedEventArgs e) {
        try { Clipboard.SetText("chrome://extensions/"); } catch { }
        string extensionPath = Path.Combine(AppContext.BaseDirectory, "chrome-extension");
        if (Directory.Exists(extensionPath)) Process.Start(new ProcessStartInfo { FileName = "explorer.exe", ArgumentList = { extensionPath }, UseShellExecute = true });
        MessageBox.Show("Chrome 확장 관리 주소가 클립보드에 복사되었습니다.\n\n1. Chrome 주소창에서 Ctrl+V로 chrome://extensions/를 엽니다.\n2. 개발자 모드를 켭니다.\n3. 처음 설치한다면 '압축해제된 확장 프로그램을 로드합니다'를 선택하고, 방금 열린 chrome-extension 폴더를 지정합니다.\n4. 이미 설치했다면 GMTFV 확장 카드의 새로고침 버튼을 누릅니다.", "Chrome 확장 설치", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
