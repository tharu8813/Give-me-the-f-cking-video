using System.Windows;
using System.Windows.Controls;
namespace GMTFV.Wpf.Views;
public partial class DownloadItemView : UserControl
{
    public DownloadItemView() => InitializeComponent();
    private void ThumbnailFailed(object sender, ExceptionRoutedEventArgs e) { ((Image)sender).SetCurrentValue(Image.SourceProperty, null); e.Handled = true; }
}
