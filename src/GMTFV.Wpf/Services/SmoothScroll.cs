using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace GMTFV.Wpf.Services;

public static class SmoothScroll
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(SmoothScroll), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached("Offset", typeof(double), typeof(SmoothScroll), new PropertyMetadata(0d, (d, e) => ((ScrollViewer)d).ScrollToVerticalOffset((double)e.NewValue)));
    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);
    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer viewer) return;
        if ((bool)e.NewValue) {
            viewer.PreviewMouseWheel += Wheel; viewer.PreviewMouseDown += Stop; viewer.PreviewKeyDown += Stop; viewer.Unloaded += Stop;
        }
        else {
            viewer.PreviewMouseWheel -= Wheel; viewer.PreviewMouseDown -= Stop; viewer.PreviewKeyDown -= Stop; viewer.Unloaded -= Stop;
            Stop(viewer, null!);
        }
    }
    private static void Stop(object sender, RoutedEventArgs e)
    {
        var viewer = (ScrollViewer)sender; double current = viewer.VerticalOffset;
        viewer.BeginAnimation(OffsetProperty, null); viewer.SetValue(OffsetProperty, current);
    }
    private static void Wheel(object sender, MouseWheelEventArgs e)
    {
        var viewer = (ScrollViewer)sender;
        DependencyObject? target = e.OriginalSource as DependencyObject;
        while (target != null) {
            if (target is ScrollViewer candidate && candidate.IsEnabled && candidate.ScrollableHeight > 0 &&
                (e.Delta < 0 ? candidate.VerticalOffset < candidate.ScrollableHeight : candidate.VerticalOffset > 0)) break;
            target = target is Visual ? VisualTreeHelper.GetParent(target) : (target as FrameworkContentElement)?.Parent;
        }
        if (target != viewer || SystemParameters.WheelScrollLines == 0) return;
        double current = viewer.VerticalOffset;
        double distance = SystemParameters.WheelScrollLines < 0 ? viewer.ViewportHeight : SystemParameters.WheelScrollLines * 16d;
        double origin = viewer.HasAnimatedProperties ? (double)viewer.GetAnimationBaseValue(OffsetProperty) : current;
        double next = Math.Clamp(origin - e.Delta / 120d * distance, 0, viewer.ScrollableHeight);
        viewer.SetValue(OffsetProperty, next);
        if (!Motion.ReduceMotion && SystemParameters.ClientAreaAnimation)
            viewer.BeginAnimation(OffsetProperty, new DoubleAnimation(current, next, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        else { viewer.BeginAnimation(OffsetProperty, null); viewer.ScrollToVerticalOffset(next); }
        e.Handled = true;
    }
}
