using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GMTFV.Wpf.ViewModels;

public sealed class AsyncRelayCommand : ICommand {
    private readonly Func<object?, Task> executeAsync;
    private readonly Func<object?, bool>? canExecute;
    private bool isExecuting;

    public AsyncRelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null) {
        this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !isExecuting && (canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter) {
        if (!CanExecute(parameter)) return;
        isExecuting = true;
        RaiseCanExecuteChanged();
        try {
            await executeAsync(parameter);
        } finally {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
