using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class AddVideoWindow : Window
{
    public string VideoUrl => UrlTextBox.Text.Trim();

    public AddVideoWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { UrlTextBox.SelectAll(); UrlTextBox.Focus(); };
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(VideoUrl, UriKind.Absolute, out var uri) || (!uri.Host.EndsWith("youtube.com") && uri.Host != "youtu.be"))
        {
            MessageBox.Show("유효한 YouTube 주소를 입력해 주세요.", "주소 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
            UrlTextBox.Focus();
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
