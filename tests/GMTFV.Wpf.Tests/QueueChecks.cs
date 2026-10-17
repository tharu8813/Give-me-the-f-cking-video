using System.IO;
using System.Windows;
using System.Windows.Controls;
using GMTFV.Core;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.Views;

internal static partial class Program
{
    private static async Task CheckQueueOrderAsync()
    {
        var backend = new FakeBackend();
        using var vm = Create(backend, "queue-order");
        await vm.AddUrlsAsync("https://youtu.be/abcdefghijk https://youtu.be/lmnopqrstuv https://youtu.be/12345678901 https://youtu.be/98765432109 https://youtu.be/zyxwvutsrqp");
        var original = vm.Items.ToArray();
        var window = new MainWindow(vm) { Width = 900, Height = 660, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -30000, Top = -30000 };
        window.Show();
        try {
            await LayoutAsync(window); Motion.ReduceMotion = false; await Task.Delay(400);
            var list = Find<ListBox>(window).Single();
            double top = list.TransformToAncestor(window).Transform(new Point()).Y, height = list.ActualHeight;
            double movement = 0;
            foreach (var item in vm.Items.Take(2)) {
                list.SelectedItems.Add(item);
                for (int frame = 0; frame < 12; frame++) {
                    await Task.Delay(20);
                    movement = Math.Max(movement, Math.Abs(list.TransformToAncestor(window).Transform(new Point()).Y - top));
                }
            }
            Console.WriteLine($"METRIC selection viewport movement={movement:F2}px, height delta={list.ActualHeight - height:F2}px");
            Check(movement < .5 && Math.Abs(list.ActualHeight - height) < .5, "single and multiple selection keep the queue viewport stationary throughout the animation");
            Check(vm.Items.Select(i => i.QueueNumber).SequenceEqual(Enumerable.Range(1, 5)), "queue numbers start at one in visible order");
            Check(!vm.MoveUpCommand.CanExecute(list.SelectedItems), "top selected block cannot move above the first item");
            var up = Find<Button>(window).Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == "선택한 항목 위로 이동");
            var down = Find<Button>(window).Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == "선택한 항목 아래로 이동");
            Check(!up.IsEnabled && down.IsEnabled, "move button availability reflects the current selection boundaries");
            vm.MoveDownCommand.Execute(list.SelectedItems); await Task.Delay(400);
            Check(vm.Items.SequenceEqual(new[] { original[2], original[0], original[1], original[3], original[4] }), "moving a selected block down preserves its internal order");
            Check(list.SelectedItems.Cast<object>().ToHashSet().SetEquals(original.Take(2)), "reordering retains the live multi-selection");
            vm.MoveUpCommand.Execute(list.SelectedItems);
            for (int i = 0; i < 10; i++) { vm.MoveDownCommand.Execute(list.SelectedItems); vm.MoveUpCommand.Execute(list.SelectedItems); }
            await Task.Delay(400);
            Check(vm.Items.SequenceEqual(original) && list.SelectedItems.Count == 2, "rapid repeated moves settle without changing selection or item order");
            list.UnselectAll(); list.SelectedItems.Add(original[1]); list.SelectedItems.Add(original[3]);
            vm.MoveUpCommand.Execute(list.SelectedItems);
            Check(vm.Items.SequenceEqual(new[] { original[1], original[0], original[3], original[2], original[4] }), "nonadjacent selections move one step without reversing");
            vm.MoveDownCommand.Execute(list.SelectedItems);
            list.SelectAll();
            Check(!vm.MoveUpCommand.CanExecute(list.SelectedItems) && !vm.MoveDownCommand.CanExecute(list.SelectedItems), "selecting the entire queue disables both move directions");
            list.UnselectAll(); list.SelectedItem = original[^1];
            Check(!vm.MoveDownCommand.CanExecute(list.SelectedItems), "last item cannot move below the queue");
            vm.MoveUpCommand.Execute(list.SelectedItems);
            original[0].Options = new() { IsVideo = false, Format = "flac" };
            string saved = Path.Combine(Root, "reordered.json"); await vm.SaveListAsync(saved);
            using (var restored = Create(new FakeBackend(), "reordered-restore")) {
                await restored.LoadListAsync(saved);
                Check(restored.Items.Select(i => i.Url).SequenceEqual(vm.Items.Select(i => i.Url)) && restored.Items[0].Options is { Format: "flac" } && restored.Items.Select(i => i.QueueNumber).SequenceEqual(Enumerable.Range(1, 5)), "saved lists restore the new order, per-item options and consecutive numbers");
            }
            await vm.SaveListAsync(Path.Combine(Root, "reordered.txt"));
            Check(File.ReadAllLines(Path.Combine(Root, "reordered.txt")).SequenceEqual(vm.Items.Select(i => i.Url)), "text list export preserves reordered URLs");
            vm.Settings.OutputFolder = Path.Combine(Root, "ordered-output"); vm.Settings.Concurrency = 1; vm.Settings.FileNameTemplate = "%num2%_%id%";
            backend.Slow = true; var snapshot = vm.Items.ToArray();
            Task downloading = vm.StartAsync(); await Task.Delay(30);
            vm.MoveUpCommand.Execute(list.SelectedItems);
            Check(vm.IsRunning && !vm.MoveUpCommand.CanExecute(list.SelectedItems) && vm.Items.SequenceEqual(snapshot), "active downloads reject reordering and preserve their request snapshot");
            vm.CancelAll(); await downloading;
            backend.Slow = false; backend.Requests.Clear(); foreach (var item in vm.Items) item.State = DownloadState.Ready;
            await vm.StartAsync();
            Check(vm.Items.Select((item, index) => Path.GetFileName(backend.Requests.Single(r => r.Url == item.Url).OutputPath).StartsWith($"{index + 1:D2}_")).All(v => v), "download request sequence follows the reordered queue");
            vm.RemoveSelectedCommand.Execute(new[] { original[1] });
            Check(!vm.MoveUpCommand.CanExecute(list.SelectedItems), "pending removal blocks reordering until collection mutation finishes");
            await Task.Delay(240);
            Check(vm.Items.Count == 4 && vm.Items.Select(i => i.QueueNumber).SequenceEqual(Enumerable.Range(1, 4)), "removing an item closes the numbering gap");
            list.UnselectAll(); list.SelectedItems.Add(vm.Items[0]); list.SelectedItems.Add(vm.Items[1]);
            window.Width = 1100; window.Height = 780;
            await Capture(window, Path.Combine("artifacts", "wpf-preview", "queue-order.png"));
        }
        finally { Motion.ReduceMotion = true; await vm.StopAsync(); window.Close(); await Task.Delay(80); }
    }
}
