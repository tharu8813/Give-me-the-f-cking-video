using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using GMTFV.Wpf.ViewModels;
namespace GMTFV.Wpf.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
    private bool closing;
    private bool closeAllowed;
    private UIElement? workspaceFocus, returnFocus;
    private bool overlayOpen;
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        MinWidth = Math.Min(MinWidth, SystemParameters.WorkArea.Width);
        MinHeight = Math.Min(MinHeight, SystemParameters.WorkArea.Height);
        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        DataContext = viewModel;
        PreviewGotKeyboardFocus += (_, e) => {
            if (ViewModel.IsWorkspaceEnabled && e.NewFocus is UIElement element) workspaceFocus = element;
        };
        SourceInitialized += (_, _) => {
            // Windows 10 keeps system chrome; these documented caption attributes require Windows 11.
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int dark = 1, caption = 0x161717, text = 0xE7F0F4;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
            DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
            DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
        };
        viewModel.PropertyChanged += PanelChanged;
        Closed += (_, _) => viewModel.PropertyChanged -= PanelChanged;
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    private void PanelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(MainWindowViewModel.Panel) or nameof(MainWindowViewModel.Failure) or nameof(MainWindowViewModel.ItemSettings))) return;
        bool open = ViewModel.HasPanel || ViewModel.Failure != null || ViewModel.ItemSettings != null;
        if (open && !overlayOpen) returnFocus = workspaceFocus;
        overlayOpen = open;
        // Wait for command availability to refresh before restoring a row's options button.
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => {
            if (closing) return;
            if (ViewModel.Failure != null) FailureOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            else if (ViewModel.ItemSettings != null) ItemOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            else if (ViewModel.HasPanel) PanelOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            else if (returnFocus is { IsVisible: true, IsEnabled: true } && returnFocus.IsDescendantOf(this)) returnFocus.Focus();
            else UrlBox.Focus();
        });
    }
    private async void WindowDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (closing || !ViewModel.IsWorkspaceEnabled) return;
        try {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
                    if (IsListFile(path)) await ViewModel.LoadListAsync(path);
            }
            else if (e.Data.GetDataPresent(DataFormats.UnicodeText))
                await ViewModel.AddUrlsAsync((string)e.Data.GetData(DataFormats.UnicodeText));
        }
        catch (Exception ex) { ViewModel.ReportError(ex); }
    }
    private static bool IsListFile(string path) => System.IO.Path.GetExtension(path).ToLowerInvariant() is ".json" or ".txt";
    private void WindowDragOver(object sender, DragEventArgs e)
    {
        bool supported = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? ((string[])e.Data.GetData(DataFormats.FileDrop)).Any(IsListFile)
            : e.Data.GetDataPresent(DataFormats.UnicodeText);
        e.Effects = !closing && ViewModel.IsWorkspaceEnabled && supported ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private async void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (closing) return;
        if (e.Key == Key.Escape) {
            if (ViewModel.Failure != null) ViewModel.Failure = null;
            else if (ViewModel.ItemSettings != null) ViewModel.CloseItemCommand.Execute(null);
            else if (ViewModel.HasPanel) ViewModel.Panel = "";
            else return;
            e.Handled = true; return;
        }
        if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control && ViewModel.IsWorkspaceEnabled) { UrlBox.Focus(); e.Handled = true; return; }
        if (e.Key == Key.Enter && UrlBox.IsKeyboardFocusWithin && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { e.Handled = true; if (ViewModel.AddCommand.CanExecute(null)) ViewModel.AddCommand.Execute(null); return; }
        if (e.Key != Key.V || Keyboard.Modifiers != ModifierKeys.Control || Keyboard.FocusedElement is TextBox || !ViewModel.IsWorkspaceEnabled) return;
        e.Handled = true;
        try { if (Clipboard.ContainsText()) await ViewModel.AddUrlsAsync(Clipboard.GetText()); }
        catch (Exception ex) { ViewModel.ReportError(ex); }
    }
    private void ClosePanel(object sender, MouseButtonEventArgs e) => ViewModel.Panel = "";
    private void CloseFailure(object sender, RoutedEventArgs e) => ViewModel.Failure = null;
    private void RetryFailure(object sender, RoutedEventArgs e) => Dispatcher.BeginInvoke(() => ViewModel.Failure = null);
    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (closeAllowed) return;
        e.Cancel = true;
        if (closing) return;
        closing = true;
        IsEnabled = false;
        await Task.Yield();
        try { await ViewModel.StopAsync(); }
        catch (Exception ex) { ViewModel.ReportError(ex); }
        finally { ViewModel.Dispose(); closeAllowed = true; Close(); }
    }
}
