using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using GMTFV.Core;
using GMTFV.Wpf.Views;

internal static partial class Program
{
    private static async Task CheckUiUxAsync()
    {
        using var vm = Create(new FakeBackend(), "ui-ux");
        vm.Settings.OutputFolder = Path.Combine(Root, "내 영상", "저장할 플레이리스트");
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv");
        var window = new MainWindow(vm) { ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show();
        string previews = Path.GetFullPath(Path.Combine("artifacts", "ui-ux-preview"));
        Directory.CreateDirectory(previews);
        var queue = Find<QueueView>(window).Single();
        var list = Find<ListBox>(queue).Single();
        var selection = (Border)queue.FindName("SelectionBar");
        var folder = Find<Button>(window).Single(b => AutomationProperties.GetName(b) == "저장 위치 선택");
        var format = Find<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "기본 저장 형식");
        var bitrate = Find<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "변환 시 비트레이트");
        await LayoutAsync(window);
        var failedCount = Find<TextBlock>(window).Single(t => t.Text == "실패 0개");
        Check(!failedCount.IsVisible, "a healthy queue does not display a red zero-failures warning");
        vm.Items[1].State = DownloadState.Failed; await LayoutAsync(window);
        Check(failedCount.IsVisible && failedCount.Text == "실패 1개", "a real failure immediately exposes the failure count");
        vm.Items[1].State = DownloadState.Ready;
        Rect Bounds(FrameworkElement element) => element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize));
        foreach (var size in new[] { (900d, 660d), (1050d, 700d), (1220d, 800d) }) {
            window.Width = size.Item1; window.Height = size.Item2; await LayoutAsync(window);
            Check(folder.ActualWidth >= 240 && Find<TextBlock>(folder).Single().ActualWidth >= 190,
                $"{size.Item1}px window leaves readable space for the save location");
            var quality = Find<ComboBox>(window).Single(c => AutomationProperties.GetName(c) == "기본 화질");
            Check(Bounds(format).Right <= Bounds(quality).Left && Bounds(quality).Right <= Bounds(folder).Left,
                $"{size.Item1}px format, quality and save location do not overlap");
            double queueTop = Bounds(list).Top, queueHeight = list.ActualHeight;
            list.SelectAll(); await LayoutAsync(window);
            var summary = (DockPanel)queue.FindName("QueueSummary");
            Check(Bounds(summary).Right <= Bounds(selection).Left && Bounds(selection).Right <= Bounds(queue).Right,
                $"{size.Item1}px selection actions do not overlap the queue summary");
            Check(Math.Abs(Bounds(list).Top - queueTop) < .5 && Math.Abs(list.ActualHeight - queueHeight) < .5,
                $"{size.Item1}px selection keeps the queue viewport stationary");
            Check(Find<Button>(selection).All(b => Bounds(b).Left >= Bounds(selection).Left && Bounds(b).Right <= Bounds(selection).Right),
                $"{size.Item1}px all selection actions remain reachable");
            await Capture(window, Path.Combine(previews, $"selection-{size.Item1}.png"));
            list.UnselectAll(); await LayoutAsync(window);
            Check(list.ActualHeight >= 2 * Find<ListBoxItem>(list).First().ActualHeight,
                $"{size.Item1}px unselected queue still shows two complete rows");
            await Capture(window, Path.Combine(previews, $"queue-{size.Item1}.png"));
        }

        vm.Settings.IsVideo = false; vm.SelectedFormat = "original"; await LayoutAsync(window);
        Check(!bitrate.IsEnabled && ToolTipService.GetShowOnDisabled(bitrate) && !string.IsNullOrWhiteSpace(bitrate.ToolTip?.ToString()),
            "disabled original-audio bitrate retains its explanatory tooltip");
        await Capture(window, Path.Combine(previews, "original-audio.png"));
        vm.Settings.IsVideo = true;
        window.Activate(); format.Focus(); await LayoutAsync(window);
        Check(format.IsKeyboardFocusWithin, "keyboard focus can start on a quick option");
        vm.Panel = "Settings"; await LayoutAsync(window);
        Check(((FrameworkElement)window.FindName("PanelOverlay")).IsKeyboardFocusWithin, "opening settings moves keyboard focus into the panel");
        vm.Failure = vm.Items[0]; vm.Items[0].ErrorKind = DownloadError.Network; vm.Items[0].Error = "테스트 연결 오류";
        await LayoutAsync(window);
        Check(((FrameworkElement)window.FindName("FailureOverlay")).IsKeyboardFocusWithin, "a nested failure dialog receives keyboard focus");
        vm.Failure = null; await LayoutAsync(window);
        Check(((FrameworkElement)window.FindName("PanelOverlay")).IsKeyboardFocusWithin, "closing the top dialog restores focus to the settings panel");
        vm.Panel = ""; await LayoutAsync(window);
        Check(format.IsKeyboardFocusWithin, "closing settings returns focus to the original quick option");

        var edit = Find<Button>(list).First(b => Equals(b.Content, "옵션 · 자막"));
        edit.Focus();
        Check(edit.IsKeyboardFocusWithin, "keyboard focus can start on an item action");
        vm.EditItemCommand.Execute(edit.DataContext); await LayoutAsync(window);
        Check(vm.ItemSettings != null, "the focused item action opens its settings");
        vm.CloseItemCommand.Execute(null); await LayoutAsync(window);
        Check(edit.IsKeyboardFocusWithin, "closing item settings returns focus to the originating row action");
        edit.Focus(); vm.EditItemCommand.Execute(edit.DataContext); await LayoutAsync(window);
        vm.Items.Remove(vm.Items[0]); await LayoutAsync(window);
        vm.CloseItemCommand.Execute(null); await LayoutAsync(window);
        Check(((TextBox)window.FindName("UrlBox")).IsKeyboardFocusWithin, "removing the original row falls back to the URL input");
        Check(Errors.Messages.Count == 0, "UI/UX interaction checks have no binding errors");
        await vm.StopAsync(); window.Close(); await Task.Delay(80);
    }
}
