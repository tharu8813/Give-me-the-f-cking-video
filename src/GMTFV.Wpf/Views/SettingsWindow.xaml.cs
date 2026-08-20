using GMTFV.Wpf.Models;
using GMTFV.Wpf.Services;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class SettingsWindow : Window {
    public SettingsWindow(DownloadSettings settings) {
        InitializeComponent();
        DarkModeWindowHelper.Apply(this);
        Settings = settings.Clone();
        DataContext = Settings;
        VideoRadio.IsChecked = Settings.IsVideo;
        AudioRadio.IsChecked = !Settings.IsVideo;
        UpdateContainerOptions();
    }

    public DownloadSettings Settings { get; }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e) {
        var dialog = new OpenFolderDialog { InitialDirectory = Directory.Exists(Settings.OutputDirectory) ? Settings.OutputDirectory : null };
        if (dialog.ShowDialog() == true) Settings.OutputDirectory = dialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e) {
        try {
            if (!int.TryParse(MaxConcurrentDownloadsTextBox.Text, out int concurrentDownloads)) throw new ArgumentException("동시 다운로드 수를 숫자로 입력해주세요.");
            if (!int.TryParse(AudioBitrateTextBox.Text, out int audioBitrate)) throw new ArgumentException("오디오 비트레이트를 숫자로 입력해주세요.");
            Settings.MaxConcurrentDownloads = concurrentDownloads;
            Settings.AudioBitrate = audioBitrate;
            if (string.IsNullOrWhiteSpace(Settings.FileNameTemplate)) Settings.FileNameTemplate = "%title%_%date%";
            Settings.NormalizeAndValidate();
            DialogResult = true;
        } catch (ArgumentException ex) {
            MessageBox.Show(ex.Message, "설정", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void VideoRadio_Click(object sender, RoutedEventArgs e) {
        Settings.IsVideo = true;
        UpdateContainerOptions();
    }

    private void AudioRadio_Click(object sender, RoutedEventArgs e) {
        Settings.IsVideo = false;
        UpdateContainerOptions();
    }

    private void UpdateContainerOptions() {
        string[] options = Settings.IsVideo
            ? new[] { "mp4", "mkv", "webm" }
            : new[] { "mp3", "m4a", "opus", "wav", "flac" };
        ContainerComboBox.ItemsSource = options;
        if (!options.Contains(Settings.Container, StringComparer.OrdinalIgnoreCase)) Settings.Container = options[0];
    }
}
