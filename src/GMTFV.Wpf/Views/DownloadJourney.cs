using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GMTFV.Wpf.Services;
namespace GMTFV.Wpf.Views;

public sealed class DownloadJourney : FrameworkElement
{
    public static readonly DependencyProperty PhaseProperty = DependencyProperty.Register(nameof(Phase), typeof(int), typeof(DownloadJourney), new PropertyMetadata(0, Changed));
    private static readonly DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(double), typeof(DownloadJourney), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public int Phase { get => (int)GetValue(PhaseProperty); set => SetValue(PhaseProperty, value); }
    public static readonly DependencyProperty FailedProperty = DependencyProperty.Register(nameof(Failed), typeof(bool), typeof(DownloadJourney), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Failed { get => (bool)GetValue(FailedProperty); set => SetValue(FailedProperty, value); }
    private double Position { get => (double)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }
    public DownloadJourney() {
        Height = 18; IsHitTestVisible = false;
        Loaded += (_, _) => { Motion.PreferenceChanged += Settle; Settle(); };
        Unloaded += (_, _) => { Motion.PreferenceChanged -= Settle; BeginAnimation(PositionProperty, null); };
    }
    private void Settle() { double target = Phase < 0 ? Position : Phase; BeginAnimation(PositionProperty, null); Position = target; }
    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var view = (DownloadJourney)d;
        if (view.Phase < 0) { view.Settle(); return; }
        if (!Motion.Enabled || !view.IsLoaded) { view.Settle(); return; }
        view.BeginAnimation(PositionProperty, new DoubleAnimation(view.Position, view.Phase, TimeSpan.FromMilliseconds(Motion.Spatial)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
    }
    protected override void OnRender(DrawingContext dc) {
        base.OnRender(dc);
        double step = Math.Max(0, ActualWidth - 12) / 4;
        Brush line = (Brush)FindResource("LineBrush"), accent = (Brush)FindResource(Phase < 0 ? Failed ? "ErrorBrush" : "MutedBrush" : Phase == 4 ? "SuccessBrush" : "AccentBrush"), surface = (Brush)FindResource("PanelBrush");
        dc.DrawLine(new Pen(line, 2), new Point(6, 9), new Point(ActualWidth - 6, 9));
        dc.DrawLine(new Pen(accent, 2), new Point(6, 9), new Point(6 + step * Position, 9));
        for (int i = 0; i < 5; i++) dc.DrawEllipse(i <= Position ? accent : surface, new Pen(i <= Position ? accent : line, 1.5), new Point(6 + i * step, 9), 3, 3);
        dc.DrawEllipse(surface, new Pen(accent, 1.5), new Point(6 + step * Position, 9), 5, 5);
        dc.DrawEllipse(accent, null, new Point(6 + step * Position, 9), 2, 2);
    }
}
