// ============================================================================
//  RelayCommand.cs
//  ICommand implementation cho MVVM
//  Namespace : SSSW.UI.WPF.ViewModels
// ============================================================================
using System.Windows.Input;

namespace SSSW.UI.WPF.ViewModels
{
    /// <summary>
    /// Relay / delegate command dùng chung cho toàn bộ ViewModel.
    /// Hỗ trợ cả dạng có tham số (object?) và không tham số.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>   _execute;
        private readonly Func<object?, bool>? _canExecute;

        // ── Constructor có tham số ────────────────────────────────────────────
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // ── Constructor không tham số (tiện lợi) ─────────────────────────────
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

        // ── ICommand ─────────────────────────────────────────────────────────
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>Force WPF re-evaluate CanExecute cho tất cả commands.</summary>
        public static void RaiseAll() => CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>Async relay command bọc Task (fire-and-forget với exception logging).</summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
            => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try   { await _execute(parameter); }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
