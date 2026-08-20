using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GMTFV.Wpf.Behaviors;

/// <summary>마우스 휠 입력을 짧은 픽셀 단위 애니메이션으로 변환합니다.</summary>
public static class SmoothScrollBehavior {
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(SmoothScrollBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(ScrollState),
        typeof(SmoothScrollBehavior));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
        if (dependencyObject is not FrameworkElement element) return;
        element.Loaded -= Element_Loaded;
        element.Unloaded -= Element_Unloaded;
        Detach(element);
        if (e.NewValue is true) {
            element.Loaded += Element_Loaded;
            element.Unloaded += Element_Unloaded;
            if (element.IsLoaded) Attach(element);
        }
    }

    private static void Element_Loaded(object sender, RoutedEventArgs e) {
        if (sender is FrameworkElement element) Attach(element);
    }

    private static void Element_Unloaded(object sender, RoutedEventArgs e) {
        if (sender is FrameworkElement element) Detach(element);
    }

    private static void Attach(FrameworkElement element) {
        Detach(element);
        ScrollViewer? viewer = element as ScrollViewer ?? FindVisualChild<ScrollViewer>(element);
        if (viewer is null) return;
        var state = new ScrollState(element, viewer);
        element.SetValue(StateProperty, state);
        state.Attach();
    }

    private static void Detach(FrameworkElement element) {
        if (element.GetValue(StateProperty) is not ScrollState state) return;
        state.Detach();
        element.ClearValue(StateProperty);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++) {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) return match;
            T? descendant = FindVisualChild<T>(child);
            if (descendant is not null) return descendant;
        }
        return null;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject {
        while (child is not null) {
            if (child is T match) return match;
            try {
                child = VisualTreeHelper.GetParent(child);
            } catch (InvalidOperationException) {
                child = LogicalTreeHelper.GetParent(child);
            }
        }
        return null;
    }

    private sealed class ScrollState {
        private const double WheelDistanceFactor = 0.65;
        private const double AnimationDurationMilliseconds = 170;
        private readonly FrameworkElement element;
        private readonly ScrollViewer viewer;
        private readonly DispatcherTimer timer;
        private readonly Stopwatch stopwatch = new();
        private double startOffset;
        private double targetOffset;

        public ScrollState(FrameworkElement element, ScrollViewer viewer) {
            this.element = element;
            this.viewer = viewer;
            targetOffset = viewer.VerticalOffset;
            timer = new DispatcherTimer(DispatcherPriority.Input, element.Dispatcher) { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += Timer_Tick;
        }

        public void Attach() => element.PreviewMouseWheel += Element_PreviewMouseWheel;

        public void Detach() {
            element.PreviewMouseWheel -= Element_PreviewMouseWheel;
            timer.Stop();
            stopwatch.Stop();
        }

        private void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (viewer.ScrollableHeight <= 0) return;

            if (e.OriginalSource is DependencyObject source) {
                ScrollViewer? nearestViewer = FindVisualParent<ScrollViewer>(source);
                if (nearestViewer is not null && nearestViewer != viewer && nearestViewer.ScrollableHeight > 0) return;
            }

            double baseOffset = timer.IsEnabled ? targetOffset : viewer.VerticalOffset;
            double nextOffset = Math.Clamp(baseOffset - e.Delta * WheelDistanceFactor, 0, viewer.ScrollableHeight);
            if (Math.Abs(nextOffset - baseOffset) < 0.1) return;

            e.Handled = true;
            startOffset = viewer.VerticalOffset;
            targetOffset = nextOffset;
            stopwatch.Restart();
            if (!timer.IsEnabled) timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e) {
            double progress = Math.Clamp(stopwatch.Elapsed.TotalMilliseconds / AnimationDurationMilliseconds, 0, 1);
            double eased = 1 - Math.Pow(1 - progress, 3);
            viewer.ScrollToVerticalOffset(startOffset + (targetOffset - startOffset) * eased);
            if (progress < 1) return;
            viewer.ScrollToVerticalOffset(targetOffset);
            timer.Stop();
            stopwatch.Stop();
        }
    }
}
