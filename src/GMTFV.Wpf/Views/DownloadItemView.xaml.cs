using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GMTFV.Wpf.Services;
namespace GMTFV.Wpf.Views;
public partial class DownloadItemView : UserControl
{
    private BitmapImage? downloading;
    private Image? target;
    public DownloadItemView() { InitializeComponent(); Unloaded += (_, _) => DetachThumbnail(); }
    private void ThumbnailChanged(object sender, DataTransferEventArgs e)
    {
        DetachThumbnail();
        var image = (Image)sender;
        if (image.Source is not BitmapImage bitmap) return;
        if (!bitmap.IsDownloading) { Motion.Reveal(image); return; }
        downloading = bitmap; target = image;
        bitmap.DownloadCompleted += ThumbnailReady;
        bitmap.DownloadFailed += ThumbnailDownloadFailed;
    }
    private void ThumbnailReady(object? sender, EventArgs e)
    {
        var image = target;
        DetachThumbnail();
        if (image != null && ReferenceEquals(image.Source, sender)) Motion.Reveal(image);
    }
    private void ThumbnailDownloadFailed(object? sender, ExceptionEventArgs e) => DetachThumbnail();
    private void DetachThumbnail()
    {
        if (downloading == null) return;
        downloading.DownloadCompleted -= ThumbnailReady; downloading.DownloadFailed -= ThumbnailDownloadFailed;
        downloading = null; target = null;
    }
    private void ThumbnailFailed(object sender, ExceptionRoutedEventArgs e) { ((Image)sender).SetCurrentValue(Image.SourceProperty, null); e.Handled = true; }
}
