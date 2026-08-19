using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();
    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
