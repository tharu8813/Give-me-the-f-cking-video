using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;
using GMTFV.Wpf.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GMTFV.Wpf.Views;

public partial class MainWindow : Window {
    private readonly MainWindowViewModel viewModel;
    private readonly ChromeTabImportService chromeTabImportService = new();
    private readonly bool isChromeTabImportAvailable;

    public MainWindow() {
        InitializeComponent();
        DarkModeWindowHelper.Apply(this);
        viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        chromeTabImportService.TabsReceived += ChromeTabImportService_TabsReceived;
        isChromeTabImportAvailable = chromeTabImportService.Start();
        if (!isChromeTabImportAvailable) viewModel.ReportChromeConnectionUnavailable();
    }

    protected override void OnClosed(EventArgs e) {
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        chromeTabImportService.Dispose();
        viewModel.Dispose();
        base.OnClosed(e);
    }

    private void Settings_Click(object sender, RoutedEventArgs e) {
        if (viewModel.IsDownloading) return;
        var window = new SettingsWindow(viewModel.Settings) { Owner = this };
        if (window.ShowDialog() == true) viewModel.ApplySettings(window.Settings);
    }

    private async void ImportPlaylist_Click(object sender, RoutedEventArgs e) {
        if (viewModel.IsDownloading) return;
        var dialog = new OpenFileDialog { Filter = "GMTFV 목록 (*.json;*.txt)|*.json;*.txt|모든 파일 (*.*)|*.*", Title = "다운로드 목록 불러오기" };
        if (dialog.ShowDialog() == true) await viewModel.ImportPlaylistAsync(dialog.FileName);
    }

    private void ExportPlaylist_Click(object sender, RoutedEventArgs e) {
        if (viewModel.IsDownloading) return;
        var dialog = new SaveFileDialog { Filter = "JSON 목록 파일 (*.json)|*.json|URL 텍스트 파일 (*.txt)|*.txt", FileName = "GMTFV_Playlist" };
        if (dialog.ShowDialog() == true) viewModel.ExportPlaylist(dialog.FileName, dialog.FilterIndex == 1);
    }

    private void DownloadList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (viewModel.IsDownloading) return;
        if (viewModel.SelectedDownload is not { } item) return;
        if (!item.CanDownload) {
            MessageBox.Show(item.IsMetadataLoading ? "영상 정보를 불러오는 중입니다. 잠시 후 다시 시도해주세요." : item.StatusText, "영상 상세 정보", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new VideoDetailsWindow(item) { Owner = this }.ShowDialog();
    }

    private void DownloadList_SelectionChanged(object sender, SelectionChangedEventArgs e) => viewModel.SetSelectedItems(GetSelectedItems());

    private void DownloadList_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
        if (viewModel.IsDownloading) e.Handled = true;
    }

    private void DownloadList_PreviewKeyDown(object sender, KeyEventArgs e) {
        if (viewModel.IsDownloading) { e.Handled = true; return; }
        if (e.Key != Key.Delete) return;
        e.Handled = true;
        DeleteSelectedItems();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e) => DeleteSelectedItems();

    private void DeleteSelectedItems() {
        DownloadItemViewModel[] selected = GetSelectedItems();
        if (selected.Length == 0) {
            MessageBox.Show("삭제할 영상을 먼저 선택해주세요. Ctrl 또는 Shift 키로 여러 항목을 선택할 수 있습니다.", "선택 삭제", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (viewModel.IsDownloading) {
            MessageBox.Show("다운로드 중에는 목록을 삭제할 수 없습니다. 작업을 취소하거나 완료한 뒤 다시 시도해주세요.", "선택 삭제", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string targetText = selected.Length == 1 ? $"‘{selected[0].Title}’ 항목을" : $"선택한 {selected.Length}개 항목을";
        MessageBoxResult result = MessageBox.Show($"{targetText} 목록에서 삭제할까요?\n\n이미 다운로드한 파일은 삭제되지 않습니다.", "선택 항목 삭제", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (result == MessageBoxResult.Yes) viewModel.RemoveItems(selected);
    }

    private void ApplyBulkProfile_Click(object sender, RoutedEventArgs e) {
        if (viewModel.IsDownloading) return;
        DownloadItemViewModel[] selected = GetSelectedItems();
        if (selected.Length == 0) {
            MessageBox.Show("설정을 적용할 영상을 먼저 선택해주세요. Ctrl 또는 Shift 키로 여러 항목을 선택할 수 있습니다.", "일괄 설정", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        bool isVideo = (BulkTypeCombo.SelectedItem as ComboBoxItem)?.Tag as string != "audio";
        string quality = (BulkQualityCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? string.Empty;
        viewModel.ApplyProfileToItems(selected, isVideo, quality, 0);
    }

    private DownloadItemViewModel[] GetSelectedItems() => DownloadList.SelectedItems.Cast<DownloadItemViewModel>().ToArray();

    private async void AddLinks_Click(object sender, RoutedEventArgs e) {
        if (viewModel.IsDownloading) return;
        var window = new AddLinksWindow { Owner = this };
        if (window.ShowDialog() == true) await viewModel.AddUrlsInBackgroundAsync(window.Urls);
    }

    private void DownloadList_DragEnter(object sender, DragEventArgs e) {
        e.Effects = !viewModel.IsDownloading && HasSupportedDropData(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DownloadList_Drop(object sender, DragEventArgs e) {
        e.Handled = true;
        if (viewModel.IsDownloading) return;

        var urls = new List<string>();
        if (TryGetDroppedText(e.Data, out string text)) urls.AddRange(AddLinksWindow.ParseUrls(text));
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files) {
            foreach (string file in files.Where(file => File.Exists(file) && IsSupportedPlaylistFile(file))) {
                await viewModel.ImportPlaylistAsync(file);
            }
        }
        if (urls.Count > 0) await viewModel.AddUrlsInBackgroundAsync(urls);
    }

    private static bool HasSupportedDropData(IDataObject data) =>
        data.GetDataPresent(DataFormats.UnicodeText) || data.GetDataPresent(DataFormats.Text) || data.GetDataPresent(DataFormats.FileDrop);

    private static bool IsSupportedPlaylistFile(string file) {
        string extension = Path.GetExtension(file);
        return extension.Equals(".json", StringComparison.OrdinalIgnoreCase) || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetDroppedText(IDataObject data, out string text) {
        text = data.GetData(DataFormats.UnicodeText) as string ?? data.GetData(DataFormats.Text) as string ?? string.Empty;
        return !string.IsNullOrWhiteSpace(text);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDownloading) && viewModel.IsDownloading) DownloadList.UnselectAll();
    }

    private void ChromeTabs_Click(object sender, RoutedEventArgs e) {
        if (viewModel.IsDownloading) return;
        new ChromeTabsWindow(chromeTabImportService, isChromeTabImportAvailable) { Owner = this }.ShowDialog();
    }

    private async void ChromeTabImportService_TabsReceived(object? sender, IReadOnlyList<string> urls) {
        await Dispatcher.InvokeAsync(() => viewModel.ImportChromeTabsAsync(urls)).Task.Unwrap();
    }
}
