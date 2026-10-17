using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace GMTFV.Wpf.Services;

// Collection changes animate realized rows; selection never moves the viewport.
public static class QueueMotion
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(QueueMotion), new PropertyMetadata(false, (d, e) => {
        if (d is ListBox list && (bool)e.NewValue) _ = new Reflow(list);
    }));
    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);
    private sealed class Reflow
    {
        private readonly ListBox list;
        private Dictionary<object, double>? before;
        private int changes;
        public Reflow(ListBox list) { this.list = list; list.Loaded += Loaded; list.Unloaded += Unloaded; }
        private void Loaded(object sender, RoutedEventArgs e) { ((INotifyCollectionChanged)list.Items).CollectionChanged += Changed; Motion.PreferenceChanged += Settle; }
        private void Unloaded(object sender, RoutedEventArgs e) { ((INotifyCollectionChanged)list.Items).CollectionChanged -= Changed; Motion.PreferenceChanged -= Settle; Settle(); }
        private IEnumerable<ListBoxItem> Realized() {
            var panel = FindPanel(list);
            return panel == null ? [] : panel.Children.OfType<ListBoxItem>().Where(i => i.IsVisible).Take(32);
        }
        private static VirtualizingStackPanel? FindPanel(DependencyObject parent) {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is VirtualizingStackPanel panel) return panel;
                if (FindPanel(child) is { } nested) return nested;
            }
            return null;
        }
        private void Settle() { foreach (var row in Realized()) { row.BeginAnimation(UIElement.OpacityProperty, null); row.Opacity = 1; row.RenderTransform = Transform.Identity; } }
        private void Changed(object? sender, NotifyCollectionChangedEventArgs e) {
            if (!Motion.Enabled || !list.IsVisible) return;
            changes += Math.Max(e.NewItems?.Count ?? 0, e.OldItems?.Count ?? 0);
            if (before != null) return;
            before = Realized().Where(r => r.DataContext != null).ToDictionary(r => r.DataContext, r => r.TransformToAncestor(list).Transform(new Point()).Y);
            list.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
                var positions = before!; before = null; int count = changes; changes = 0;
                Settle(); if (!Motion.Enabled || count > 12 || !list.IsVisible) return;
                int index = 0;
                foreach (var row in Realized()) {
                    double y = row.TransformToAncestor(list).Transform(new Point()).Y;
                    bool existing = positions.TryGetValue(row.DataContext, out double oldY);
                    double delta = existing ? oldY - y : 18;
                    if (Math.Abs(delta) < .5 || Math.Abs(delta) > list.ActualHeight) continue;
                    var translate = new TranslateTransform(); row.RenderTransform = translate;
                    var movement = new DoubleAnimation(delta, 0, TimeSpan.FromMilliseconds(Motion.Spatial)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, BeginTime = TimeSpan.FromMilliseconds(existing ? 0 : Math.Min(index++, 5) * 24) };
                    translate.BeginAnimation(TranslateTransform.YProperty, movement);
                    if (!existing) row.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Motion.State)));
                }
            });
        }
    }
}
