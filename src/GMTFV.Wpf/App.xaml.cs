using System.IO;
using System.Windows;
using GMTFV.DownloadEngine;
using GMTFV.Wpf.Services;
using GMTFV.Wpf.ViewModels;
using GMTFV.Wpf.Views;
namespace GMTFV.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var store = new SettingsStore();
        var log = new DiagnosticLog(Path.Combine(store.DirectoryPath, "Logs"));
        var viewModel = new MainWindowViewModel(new DownloadBackend(log), store, log);
        Motion.ReduceMotion = viewModel.Settings.ReduceMotion;
        MainWindow = new MainWindow(viewModel);
        MainWindow.Show();
    }
}
