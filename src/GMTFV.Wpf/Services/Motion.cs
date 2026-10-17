using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace GMTFV.Wpf.Services;

public static class Motion
{
    public static bool ReduceMotion { get; set; }
    private static bool Enabled => !ReduceMotion && SystemParameters.ClientAreaAnimation;
    public static readonly DependencyProperty FeedbackProperty = DependencyProperty.RegisterAttached("Feedback", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element || !(bool)e.NewValue) return;
        element.MouseEnter += (_, _) => Fade(element, element.Opacity, 0.94);
        element.MouseLeave += (_, _) => Fade(element, element.Opacity, 1);
        element.PreviewMouseLeftButtonDown += (_, _) => Fade(element, 1, 0.72);
        element.PreviewMouseLeftButtonUp += (_, _) => Fade(element, element.Opacity, 1);
    }));
    public static bool GetFeedback(DependencyObject d) => (bool)d.GetValue(FeedbackProperty);
    public static void SetFeedback(DependencyObject d, bool value) => d.SetValue(FeedbackProperty, value);
    private static void Fade(FrameworkElement element, double from, double to) => element.BeginAnimation(UIElement.OpacityProperty,
        Enabled ? new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(120)) : null);
    public static readonly DependencyProperty PulseProperty = DependencyProperty.RegisterAttached("Pulse", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element || !(bool)e.NewValue) return;
        element.Loaded += (_, _) => StartPulse(element);
        element.IsVisibleChanged += (_, _) => StartPulse(element);
        element.Unloaded += (_, _) => element.BeginAnimation(UIElement.OpacityProperty, null);
    }));
    public static bool GetPulse(DependencyObject d) => (bool)d.GetValue(PulseProperty);
    public static void SetPulse(DependencyObject d, bool value) => d.SetValue(PulseProperty, value);
    private static void StartPulse(FrameworkElement element) => element.BeginAnimation(UIElement.OpacityProperty,
        Enabled && element.IsVisible ? new DoubleAnimation(0.35, 0.8, TimeSpan.FromMilliseconds(800)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever } : null);
    public static readonly DependencyProperty EnterProperty = DependencyProperty.RegisterAttached("Enter", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is FrameworkElement element && (bool)e.NewValue) element.Loaded += Enter;
    }));
    public static bool GetEnter(DependencyObject d) => (bool)d.GetValue(EnterProperty);
    public static void SetEnter(DependencyObject d, bool value) => d.SetValue(EnterProperty, value);
    private static void Enter(object sender, RoutedEventArgs e) { if (sender is FrameworkElement element && element.Visibility == Visibility.Visible) Animate(element, true); }

    public static readonly DependencyProperty PresentProperty = DependencyProperty.RegisterAttached("Present", typeof(bool), typeof(Motion), new PropertyMetadata(true, PresentChanged));
    public static bool GetPresent(DependencyObject d) => (bool)d.GetValue(PresentProperty);
    public static void SetPresent(DependencyObject d, bool value) => d.SetValue(PresentProperty, value);
    private static void PresentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element) return;
        bool show = (bool)e.NewValue;
        Animate(element, show);
    }
    private static void Animate(FrameworkElement element, bool show)
    {
        if (!element.IsLoaded || !Enabled) {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 1;
            if (element.RenderTransform is TranslateTransform translate) {
                translate.BeginAnimation(TranslateTransform.YProperty, null);
                translate.Y = 0;
            }
            element.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            return;
        }
        element.Visibility = Visibility.Visible;
        element.RenderTransform = new TranslateTransform();
        var duration = TimeSpan.FromMilliseconds(show ? 220 : 140);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var opacity = new DoubleAnimation(show ? 0 : element.Opacity, show ? 1 : 0, duration) { EasingFunction = ease };
        opacity.Completed += (_, _) => { if (!show && !GetPresent(element)) element.Visibility = Visibility.Collapsed; };
        element.BeginAnimation(UIElement.OpacityProperty, opacity);
        ((TranslateTransform)element.RenderTransform).BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(show ? 12 : 0, show ? 0 : 8, duration) { EasingFunction = ease });
    }
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.RegisterAttached("Progress", typeof(double), typeof(Motion), new PropertyMetadata(0d, (d, e) => {
        if (d is ProgressBar bar) {
            double target = (double)e.NewValue;
            if (Enabled && bar.IsLoaded) bar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, new DoubleAnimation(bar.Value, target, TimeSpan.FromMilliseconds(180)));
            else { bar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null); bar.Value = target; }
        }
    }));
    public static double GetProgress(DependencyObject d) => (double)d.GetValue(ProgressProperty);
    public static void SetProgress(DependencyObject d, double value) => d.SetValue(ProgressProperty, value);
}
