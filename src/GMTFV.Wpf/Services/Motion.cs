using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace GMTFV.Wpf.Services;

public static class Motion
{
    public const int Response = 120, State = 240, Spatial = 320;
    private static bool reduceMotion;
    private static readonly List<WeakReference<FrameworkElement>> Elements = [];
    public static event Action? PreferenceChanged;
    public static bool Enabled => !ReduceMotion && SystemParameters.ClientAreaAnimation;
    public static bool ReduceMotion {
        get => reduceMotion;
        set { if (reduceMotion == value) return; reduceMotion = value; Refresh(); }
    }
    static Motion() => SystemParameters.StaticPropertyChanged += (_, e) => { if (e.PropertyName == nameof(SystemParameters.ClientAreaAnimation)) Refresh(); };
    private static readonly DependencyProperty TrackedProperty = DependencyProperty.RegisterAttached("Tracked", typeof(bool), typeof(Motion));
    private static readonly DependencyProperty RevisionProperty = DependencyProperty.RegisterAttached("Revision", typeof(int), typeof(Motion), new PropertyMetadata(0));
    private static void Track(FrameworkElement element)
    {
        if ((bool)element.GetValue(TrackedProperty)) return;
        element.SetValue(TrackedProperty, true);
        Elements.Add(new(element));
        if (Elements.Count > 256) Elements.RemoveAll(w => !w.TryGetTarget(out _));
        element.Unloaded += (_, _) => Settle(element);
    }
    private static void Refresh()
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess()) { Application.Current.Dispatcher.BeginInvoke(Refresh); return; }
        foreach (var weak in Elements.ToArray()) if (weak.TryGetTarget(out var element)) { Settle(element); if (GetPulse(element)) StartPulse(element); }
        Elements.RemoveAll(w => !w.TryGetTarget(out _));
        PreferenceChanged?.Invoke();
    }
    private static void Settle(FrameworkElement element)
    {
        element.SetValue(RevisionProperty, (int)element.GetValue(RevisionProperty) + 1);
        element.BeginAnimation(UIElement.OpacityProperty, null); element.Opacity = 1;
        if (element.RenderTransform is TransformGroup group) foreach (var transform in group.Children) {
            if (transform is TranslateTransform t) { t.BeginAnimation(TranslateTransform.XProperty, null); t.BeginAnimation(TranslateTransform.YProperty, null); t.X = GetToggle(element) ? 14 : 0; t.Y = 0; }
            if (transform is ScaleTransform s) { s.BeginAnimation(ScaleTransform.ScaleXProperty, null); s.BeginAnimation(ScaleTransform.ScaleYProperty, null); s.ScaleX = s.ScaleY = 1; }
        }
        if (element.ReadLocalValue(PresentProperty) != DependencyProperty.UnsetValue) {
            element.Visibility = GetPresent(element) ? Visibility.Visible : Visibility.Collapsed;
            element.IsHitTestVisible = GetPresent(element);
        }
        if (element is ProgressBar bar) { bar.BeginAnimation(RangeBase.ValueProperty, null); bar.Value = GetProgress(bar); }
    }
    private static TransformGroup Transforms(FrameworkElement element)
    {
        if (element.RenderTransform is TransformGroup existing && existing.Children.Count == 2 && existing.Children[0] is ScaleTransform && existing.Children[1] is TranslateTransform) return existing;
        var group = new TransformGroup(); group.Children.Add(new ScaleTransform()); group.Children.Add(new TranslateTransform());
        element.RenderTransform = group; element.RenderTransformOrigin = new Point(.5, .5); return group;
    }
    private static DoubleAnimation To(double from, double to, int duration) => new(from, to, TimeSpan.FromMilliseconds(duration)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
    public static readonly DependencyProperty FeedbackProperty = DependencyProperty.RegisterAttached("Feedback", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element || !(bool)e.NewValue) return;
        Track(element);
        element.MouseEnter += (_, _) => Feedback(element, .99, .96);
        element.MouseLeave += (_, _) => Feedback(element, 1, 1);
        element.PreviewMouseLeftButtonDown += (_, _) => Feedback(element, .96, .8);
        element.PreviewMouseLeftButtonUp += (_, _) => Feedback(element, 1, 1);
    }));
    public static bool GetFeedback(DependencyObject d) => (bool)d.GetValue(FeedbackProperty);
    public static void SetFeedback(DependencyObject d, bool value) => d.SetValue(FeedbackProperty, value);
    public static readonly DependencyProperty ToggleProperty = DependencyProperty.RegisterAttached("Toggle", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element) return;
        Track(element); var translate = (TranslateTransform)Transforms(element).Children[1]; double target = (bool)e.NewValue ? 14 : 0;
        if (Enabled && element.IsLoaded) translate.BeginAnimation(TranslateTransform.XProperty, To(translate.X, target, State));
        else { translate.BeginAnimation(TranslateTransform.XProperty, null); translate.X = target; }
    }));
    public static bool GetToggle(DependencyObject d) => (bool)d.GetValue(ToggleProperty);
    public static void SetToggle(DependencyObject d, bool value) => d.SetValue(ToggleProperty, value);
    private static void Feedback(FrameworkElement element, double scale, double opacity)
    {
        if (!Enabled) return;
        var transform = (ScaleTransform)Transforms(element).Children[0];
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, To(transform.ScaleX, scale, Response));
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, To(transform.ScaleY, scale, Response));
        element.BeginAnimation(UIElement.OpacityProperty, To(element.Opacity, opacity, Response));
    }
    public static readonly DependencyProperty PulseProperty = DependencyProperty.RegisterAttached("Pulse", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element || !(bool)e.NewValue) return;
        Track(element); element.Loaded += (_, _) => StartPulse(element); element.IsVisibleChanged += (_, _) => StartPulse(element);
    }));
    public static bool GetPulse(DependencyObject d) => (bool)d.GetValue(PulseProperty);
    public static void SetPulse(DependencyObject d, bool value) => d.SetValue(PulseProperty, value);
    private static void StartPulse(FrameworkElement element) => element.BeginAnimation(UIElement.OpacityProperty,
        Enabled && element.IsVisible ? new DoubleAnimation(.3, .65, TimeSpan.FromMilliseconds(900)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever } : null);
    public static readonly DependencyProperty EnterProperty = DependencyProperty.RegisterAttached("Enter", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element || !(bool)e.NewValue) return;
        Track(element); element.Loaded += (_, _) => { if (element.IsVisible) Animate(element, true, true); };
    }));
    public static bool GetEnter(DependencyObject d) => (bool)d.GetValue(EnterProperty);
    public static void SetEnter(DependencyObject d, bool value) => d.SetValue(EnterProperty, value);
    public static void Reveal(FrameworkElement element) { Track(element); Animate(element, true, true); }
    public static readonly DependencyProperty KindProperty = DependencyProperty.RegisterAttached("Kind", typeof(string), typeof(Motion), new PropertyMetadata("Reveal"));
    public static string GetKind(DependencyObject d) => (string)d.GetValue(KindProperty);
    public static void SetKind(DependencyObject d, string value) => d.SetValue(KindProperty, value);
    public static readonly DependencyProperty PresentProperty = DependencyProperty.RegisterAttached("Present", typeof(bool), typeof(Motion), new PropertyMetadata(true, (d, e) => {
        if (d is FrameworkElement element) { Track(element); Animate(element, (bool)e.NewValue); }
    }));
    public static bool GetPresent(DependencyObject d) => (bool)d.GetValue(PresentProperty);
    public static void SetPresent(DependencyObject d, bool value) => d.SetValue(PresentProperty, value);
    private static void Animate(FrameworkElement element, bool show, bool entering = false)
    {
        int revision = (int)element.GetValue(RevisionProperty) + 1; element.SetValue(RevisionProperty, revision);
        element.IsHitTestVisible = show;
        if (!element.IsLoaded || !Enabled) { Settle(element); element.Visibility = show ? Visibility.Visible : Visibility.Collapsed; return; }
        var group = Transforms(element); var translate = (TranslateTransform)group.Children[1]; var scale = (ScaleTransform)group.Children[0];
        bool wasHidden = entering || element.Visibility != Visibility.Visible;
        element.Visibility = Visibility.Visible;
        bool sheet = GetKind(element) == "Sheet", modal = GetKind(element) == "Modal";
        int duration = !show ? 140 : sheet ? Spatial : State;
        var opacity = To(wasHidden ? 0 : element.Opacity, show ? 1 : 0, duration);
        opacity.Completed += (_, _) => {
            if ((int)element.GetValue(RevisionProperty) != revision) return;
            element.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            element.BeginAnimation(UIElement.OpacityProperty, null); element.Opacity = 1;
        };
        element.BeginAnimation(UIElement.OpacityProperty, opacity, HandoffBehavior.SnapshotAndReplace);
        if (GetKind(element) != "Scrim") translate.BeginAnimation(sheet ? TranslateTransform.XProperty : TranslateTransform.YProperty,
            To(wasHidden ? sheet ? 48 : 14 : sheet ? translate.X : translate.Y, show ? 0 : sheet ? 48 : 8, duration));
        if (modal) {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, To(wasHidden ? .96 : scale.ScaleX, show ? 1 : .98, duration));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, To(wasHidden ? .96 : scale.ScaleY, show ? 1 : .98, duration));
        }
    }
    public static readonly DependencyProperty DepthProperty = DependencyProperty.RegisterAttached("Depth", typeof(bool), typeof(Motion), new PropertyMetadata(false, (d, e) => {
        if (d is not FrameworkElement element) return;
        Track(element); var group = Transforms(element); var scale = (ScaleTransform)group.Children[0]; var translate = (TranslateTransform)group.Children[1];
        double target = (bool)e.NewValue ? .985 : 1;
        if (!Enabled) { Settle(element); return; }
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, To(scale.ScaleX, target, Spatial));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, To(scale.ScaleY, target, Spatial));
        translate.BeginAnimation(TranslateTransform.XProperty, To(translate.X, (bool)e.NewValue ? -12 : 0, Spatial));
    }));
    public static bool GetDepth(DependencyObject d) => (bool)d.GetValue(DepthProperty);
    public static void SetDepth(DependencyObject d, bool value) => d.SetValue(DepthProperty, value);
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.RegisterAttached("Progress", typeof(double), typeof(Motion), new PropertyMetadata(0d, (d, e) => {
        if (d is not ProgressBar bar) return;
        Track(bar); double target = Math.Clamp((double)e.NewValue, 0, 100);
        if (Enabled && bar.IsLoaded) bar.BeginAnimation(RangeBase.ValueProperty, To(bar.Value, target, State));
        else { bar.BeginAnimation(RangeBase.ValueProperty, null); bar.Value = target; }
    }));
    public static double GetProgress(DependencyObject d) => (double)d.GetValue(ProgressProperty);
    public static void SetProgress(DependencyObject d, double value) => d.SetValue(ProgressProperty, value);
}
