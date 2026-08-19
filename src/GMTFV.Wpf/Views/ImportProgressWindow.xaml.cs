using System.Windows;

namespace GMTFV.Wpf.Views;

public partial class ImportProgressWindow : Window
{
    public ImportProgressWindow() => InitializeComponent();

    public void UpdateProgress(int current, int total, string title)
    {
        CountText.Text = $"{current} / {total}";
        ImportProgress.Value = total > 0 ? current * 100d / total : 0;
        CurrentItemText.Text = title;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
