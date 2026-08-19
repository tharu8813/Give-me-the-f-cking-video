using GMTFV.Wpf.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class MainWindow : Window
{
    public ObservableCollection<DownloadItem> Items { get; } = [];
    public string QueueSummary => Items.Count == 0 ? "목록이 비어 있습니다" : $"{Items.Count}개 항목";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddVideoWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            Items.Add(new DownloadItem { Title = dialog.VideoUrl, Status = "영상 정보 확인 대기" });
            DataContext = null; DataContext = this;
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow { Owner = this }.ShowDialog();
    private void Details_Click(object sender, RoutedEventArgs e) => new VideoDetailsWindow { Owner = this }.ShowDialog();
    private void Chrome_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Chrome 탭 가져오기는 다음 단계에서 기존 로컬 확장 연동 서비스를 이 화면에 연결합니다. 확장 폴더와 기존 연동 방식은 유지됩니다.", "Chrome 탭 가져오기");
    private void Download_Click(object sender, RoutedEventArgs e) => MessageBox.Show(Items.Count == 0 ? "먼저 다운로드할 영상을 추가해 주세요." : "다운로드 엔진 연결은 마이그레이션 다음 단계에서 진행합니다.", "GMTFV");
}
