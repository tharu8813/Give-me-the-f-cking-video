using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class VideoDetailsWindow : Window
{
    public VideoDetailsWindow() => InitializeComponent();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
