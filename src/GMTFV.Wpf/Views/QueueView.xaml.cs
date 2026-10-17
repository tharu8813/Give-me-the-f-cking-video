using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GMTFV.Wpf.ViewModels;
namespace GMTFV.Wpf.Views;
public partial class QueueView : UserControl
{
    public QueueView() => InitializeComponent();
    private void QueueSelectionChanged(object sender, SelectionChangedEventArgs e) => CommandManager.InvalidateRequerySuggested();
    private void ClearSelection(object sender, RoutedEventArgs e) { QueueList.UnselectAll(); QueueList.Focus(); }
    private void QueueKeyDown(object sender, KeyEventArgs e) {
        if (Keyboard.FocusedElement is TextBox || DataContext is not MainWindowViewModel vm || !vm.IsWorkspaceEnabled) return;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Keyboard.Modifiers == ModifierKeys.Alt && key is Key.Up or Key.Down) {
            var command = key == Key.Up ? vm.MoveUpCommand : vm.MoveDownCommand;
            if (command.CanExecute(QueueList.SelectedItems)) command.Execute(QueueList.SelectedItems);
            e.Handled = true;
        }
        else if (key == Key.Escape && QueueList.SelectedItems.Count > 0) { QueueList.UnselectAll(); e.Handled = true; }
        else if (key == Key.Delete) { vm.RemoveSelectedCommand.Execute(QueueList.SelectedItems); e.Handled = true; }
    }
}
