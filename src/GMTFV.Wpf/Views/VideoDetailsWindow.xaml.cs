using GMTFV.Core;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GMTFV.Wpf.Views;

public partial class VideoDetailsWindow : Window {
    private readonly YtDlpSubtitleService subtitleService = new();
    private readonly DownloadItemViewModel item;
    private bool controlsReady;

    public VideoDetailsWindow(DownloadItemViewModel item) {
        this.item = item ?? throw new ArgumentNullException(nameof(item));
        InitializeComponent();
        DarkModeWindowHelper.Apply(this);
        DataContext = item;
        DownloadTypeCombo.ItemsSource = new[] { new OutputTypeChoice("비디오", true), new OutputTypeChoice("오디오만", false) };
        DownloadTypeCombo.SelectedIndex = item.OutputProfile.IsVideo ? 0 : 1;
        controlsReady = true;
        RefreshProfileControls();
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(item.SourceUrl);

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void DownloadType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (controlsReady) RefreshProfileControls();
    }

    private void RefreshProfileControls() {
        bool isVideo = DownloadTypeCombo.SelectedItem is OutputTypeChoice { IsVideo: true };
        if (isVideo) {
            QualityChoice[] qualities = new[] { new QualityChoice("최고 화질 (자동)", string.Empty, 0) }
                .Concat(item.AvailableQualities.Select(quality => new QualityChoice(quality.DisplayText, $"{quality.Height}p", quality.FramesPerSecond)))
                .ToArray();
            QualityCombo.ItemsSource = qualities;
            QualityCombo.IsEnabled = true;
            QualityCombo.SelectedItem = qualities.FirstOrDefault(quality =>
                string.Equals(quality.Quality, item.OutputProfile.Quality, StringComparison.OrdinalIgnoreCase) &&
                (item.OutputProfile.FramesPerSecond <= 0 || quality.FramesPerSecond == item.OutputProfile.FramesPerSecond)) ?? qualities[0];
            string[] containers = { "mp4", "mkv", "webm" };
            ContainerCombo.ItemsSource = containers;
            ContainerCombo.SelectedItem = containers.Contains(item.OutputProfile.Container, StringComparer.OrdinalIgnoreCase) ? item.OutputProfile.Container.ToLowerInvariant() : "mp4";
        } else {
            QualityCombo.ItemsSource = new[] { new QualityChoice("오디오 전용", string.Empty, 0) };
            QualityCombo.SelectedIndex = 0;
            QualityCombo.IsEnabled = false;
            string[] containers = { "mp3", "m4a", "opus", "wav", "flac" };
            ContainerCombo.ItemsSource = containers;
            ContainerCombo.SelectedItem = !item.OutputProfile.IsVideo && containers.Contains(item.OutputProfile.Container, StringComparer.OrdinalIgnoreCase) ? item.OutputProfile.Container.ToLowerInvariant() : "mp3";
        }
    }

    private void ApplyProfile_Click(object sender, RoutedEventArgs e) {
        if (DownloadTypeCombo.SelectedItem is not OutputTypeChoice type || QualityCombo.SelectedItem is not QualityChoice quality || ContainerCombo.SelectedItem is not string container) {
            ProfileStatus.Text = "다운로드 타입, 화질 및 파일 형식을 모두 선택해주세요.";
            return;
        }
        item.ApplyOutputProfile(new DownloadProfile {
            IsVideo = type.IsVideo,
            Container = container,
            Quality = type.IsVideo ? quality.Quality : string.Empty,
            FramesPerSecond = type.IsVideo ? quality.FramesPerSecond : 0
        });
        ProfileStatus.Text = $"적용 완료 · {item.ProfileSummary}";
    }

    private async void LoadSubtitles_Click(object sender, RoutedEventArgs e) {
        if (sender is not Button button) return;
        button.IsEnabled = false;
        try {
            SubtitleStatus.Text = "자막 목록을 불러오는 중…";
            IReadOnlyList<SubtitleOption> subtitles = await subtitleService.GetAvailableAsync(item.SourceUrl, CancellationToken.None);
            SubtitleList.ItemsSource = subtitles;
            SubtitleStatus.Text = subtitles.Count == 0 ? "사용 가능한 제공 자막 또는 자동 생성 자막이 없습니다." : $"자막 {subtitles.Count}개를 찾았습니다.";
        } catch (Exception ex) {
            SubtitleStatus.Text = "자막 조회 실패: " + ex.Message;
        } finally {
            button.IsEnabled = true;
        }
    }

    private async void DownloadSubtitle_Click(object sender, RoutedEventArgs e) {
        if (SubtitleList.SelectedItem is not SubtitleOption subtitle) { SubtitleStatus.Text = "저장할 자막을 선택해주세요."; return; }
        var dialog = new SaveFileDialog { Filter = "SRT 자막 (*.srt)|*.srt", FileName = item.Title + "." + subtitle.LanguageCode + ".srt" };
        if (dialog.ShowDialog() != true) return;
        try {
            SubtitleStatus.Text = "자막을 저장하는 중…";
            string template = Path.Combine(Path.GetDirectoryName(dialog.FileName)!, Path.GetFileNameWithoutExtension(dialog.FileName) + ".%(ext)s");
            await subtitleService.DownloadAsync(item.SourceUrl, subtitle, template, CancellationToken.None);
            SubtitleStatus.Text = "자막 저장을 완료했습니다.";
        } catch (Exception ex) { SubtitleStatus.Text = "자막 저장 실패: " + ex.Message; }
    }
}

public sealed record OutputTypeChoice(string DisplayName, bool IsVideo) {
    public override string ToString() => DisplayName;
}

public sealed record QualityChoice(string DisplayText, string Quality, int FramesPerSecond) {
    public override string ToString() => DisplayText;
}
