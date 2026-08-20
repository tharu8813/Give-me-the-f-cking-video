using GMTFV.Core;
using GMTFV.Wpf.Services;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class AddLinksWindow : Window {
    private static readonly Regex UrlPattern = new("https?://[^\\s<>\\\"']+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AddLinksWindow() {
        InitializeComponent();
        DarkModeWindowHelper.Apply(this);
        Loaded += (_, _) => LinksTextBox.Focus();
    }

    public IReadOnlyList<string> Urls { get; private set; } = Array.Empty<string>();

    public static IReadOnlyList<string> ParseUrls(string? text) {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        IEnumerable<string> values;
        try { values = JsonSerializer.Deserialize<string[]>(text) ?? Array.Empty<string>(); }
        catch (JsonException) { values = UrlPattern.Matches(text).Select(match => match.Value.TrimEnd('.', ',', ';', ')', ']', '}')); }
        return values.Select(value => YouTubeUrl.TryNormalize(value, out string normalized) ? normalized : null)
            .Where(value => value is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void Paste_Click(object sender, RoutedEventArgs e) {
        try {
            string clipboard = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboard)) { ValidationText.Text = "클립보드에 붙여넣을 텍스트가 없습니다."; return; }
            if (LinksTextBox.Text.Length > 0 && !LinksTextBox.Text.EndsWith(Environment.NewLine)) LinksTextBox.AppendText(Environment.NewLine);
            LinksTextBox.AppendText(clipboard);
            LinksTextBox.CaretIndex = LinksTextBox.Text.Length;
            ValidationText.Text = $"인식 가능한 YouTube 링크 {ParseUrls(LinksTextBox.Text).Count}개";
        } catch (Exception ex) { ValidationText.Text = "클립보드를 읽지 못했습니다: " + ex.Message; }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) { LinksTextBox.Clear(); LinksTextBox.Focus(); ValidationText.Text = "내용을 지웠습니다."; }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void Add_Click(object sender, RoutedEventArgs e) {
        Urls = ParseUrls(LinksTextBox.Text);
        if (Urls.Count == 0) { ValidationText.Text = "추가할 수 있는 YouTube 링크를 찾지 못했습니다."; return; }
        DialogResult = true;
        Close();
    }
}
