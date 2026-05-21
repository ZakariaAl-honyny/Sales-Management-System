using SalesSystem.Contracts.Common;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Base ViewModel using INotifyPropertyChanged and INotifyDataErrorInfo for real-time validation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public event Action? CloseRequested;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            return Enumerable.Empty<string>();
        return _errors[propertyName];
    }

    protected void AddError(string propertyName, string errorMessage)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = new List<string>();

        if (!_errors[propertyName].Contains(errorMessage))
        {
            _errors[propertyName].Add(errorMessage);
            OnErrorsChanged(propertyName);
        }
    }

    protected void ClearErrors(string propertyName)
    {
        if (_errors.ContainsKey(propertyName))
        {
            _errors.Remove(propertyName);
            OnErrorsChanged(propertyName);
        }
    }

    protected void ClearAllErrors()
    {
        var properties = _errors.Keys.ToList();
        _errors.Clear();
        foreach (var property in properties)
        {
            OnErrorsChanged(property);
        }
    }

    protected virtual void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Triggers the CloseRequested event to signal the UI to close the window
    /// </summary>
    protected void RequestClose()
    {
        CloseRequested?.Invoke();
    }

    /// <summary>
    /// Handles an exception by logging it locally and sending it to the API
    /// </summary>
    protected string HandleException(Exception ex, string context, string? logMessage = null, string? userMessage = null)
    {
        string message = logMessage ?? $"[{context}] An unexpected error occurred.";
        Serilog.Log.Error(ex, message);
        
        // Send to API in background
        _ = SendRemoteLogAsync("Error", message, ex, context);

        if (userMessage != null) return userMessage;

        string detailedError = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;

        if (ex is System.Net.Http.HttpRequestException)
            return $"فشل الاتصال بالخادم: {ex.Message}. يرجى التحقق من اتصال الإنترنت.";
        
        if (ex is TaskCanceledException || ex is TimeoutException)
            return "انتهت مهلة الطلب. يرجى المحاولة مرة أخرى.";

        return $"حدث خطأ: {detailedError}. يرجى المحاولة لاحقاً.";
    }

    /// <summary>
    /// Logs a failure result locally (for monitoring) without sending to API.
    /// These are user-facing business validation messages (duplicate names, not found, etc.)
    /// </summary>
    protected string HandleFailure(string error, string context, string? logMessage = null)
    {
        string message = logMessage ?? $"[{context}] {error}";
        Serilog.Log.Warning(message);

        // Do NOT send business validation failures to API
        // These are normal user messages, not system errors

        return error;
    }

    private async Task SendRemoteLogAsync(string level, string message, Exception? ex, string context)
    {
        try
        {
            var logsService = App.GetService<SalesSystem.DesktopPWF.Services.Api.ILogsApiService>();
            await logsService.SendLogAsync(new SalesSystem.Contracts.Requests.CreateLogRequest(
                level,
                message,
                ex?.Message,
                ex?.StackTrace,
                "Desktop",
                context,
                Environment.MachineName
            ));
        }
        catch
        {
            // Ignore failure to send log to prevent infinite loop or app crash
        }
    }

    private bool _isBusy;
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Indicates whether an async operation is currently running.
    /// Bind UI elements (Loading indicators, IsEnabled) to this.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Status message displayed during async operations.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        protected set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Wraps async operations with loading state and error handling.
    /// Use this instead of manual try/catch in every command.
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, string? busyMessage = null)
    {
        try
        {
            IsBusy = true;
            ClearAllErrors();
            if (busyMessage != null) StatusMessage = busyMessage;
            await operation();
        }
        catch (Exception ex)
        {
            HandleException(ex, "ExecuteAsync");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Wraps async operations with loading state, error handling, and a custom error callback.
    /// Use when you need to display the error message to the user.
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, Action<Exception> onError, string? busyMessage = null)
    {
        try
        {
            IsBusy = true;
            ClearAllErrors();
            if (busyMessage != null) StatusMessage = busyMessage;
            await operation();
        }
        catch (Exception ex)
        {
            onError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Wraps async operations that return Result&lt;T&gt;.
    /// Returns null if the operation failed.
    /// </summary>
    protected async Task<T?> ExecuteResultAsync<T>(Func<Task<Result<T>>> operation, string? busyMessage = null) where T : class
    {
        try
        {
            IsBusy = true;
            ClearAllErrors();
            if (busyMessage != null) StatusMessage = busyMessage;
            var result = await operation();
            if (!result.IsSuccess)
            {
                HandleFailure(result.Error, "ExecuteResultAsync");
                return null;
            }
            return result.Value;
        }
        catch (Exception ex)
        {
            HandleException(ex, "ExecuteResultAsync");
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Virtual cleanup method for ViewModels to release resources or unsubscribe from events.
    /// </summary>
    public virtual void Cleanup()
    {
    }

    /// <summary>
    /// Invokes an action on the UI thread if needed, or executes it immediately if already on the UI thread or if Application.Current is null.
    /// </summary>
    protected void InvokeOnUIThread(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// Invokes an async action on the UI thread if needed.
    /// </summary>
    protected async Task InvokeOnUIThreadAsync(Func<Task> action)
    {
        if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(action).Task;
        }
        else
        {
            await action();
        }
    }
}

/// <summary>
/// RelayCommand implementation for ICommand
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;
    private EventHandler? _canExecuteChanged;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add 
        { 
            CommandManager.RequerySuggested += value; 
            _canExecuteChanged += value;
        }
        remove 
        { 
            CommandManager.RequerySuggested -= value; 
            _canExecuteChanged -= value;
        }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Async RelayCommand
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _isExecuting;
    private EventHandler? _canExecuteChanged;

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute != null ? _ => canExecute() : null)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add 
        { 
            CommandManager.RequerySuggested += value; 
            _canExecuteChanged += value;
        }
        remove 
        { 
            CommandManager.RequerySuggested -= value; 
            _canExecuteChanged -= value;
        }
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

