using System.Windows;
using SalesSystem.DesktopPWF.Views.Dialogs;

namespace SalesSystem.DesktopPWF.Services.App;

public interface IDialogService
{
    void ShowInfo(string message, string title = "معلومات");
    void ShowWarning(string message, string title = "تنبيه");
    void ShowError(string message, string title = "خطأ");
    bool ShowConfirmation(string message, string title = "تأكيد");
    bool ShowDialog(object viewModel);
    Task ShowInfoAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<DeleteStrategy> ShowDeleteConfirmationAsync(string itemDescription);
}

public class DialogService : IDialogService
{
    public void ShowInfo(string message, string title = "معلومات")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new InfoDialog(message, title);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        });
    }

    public void ShowWarning(string message, string title = "تنبيه")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new WarningDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        });
    }

    public void ShowError(string message, string title = "خطأ")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new ErrorDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        });
    }

    public bool ShowConfirmation(string message, string title = "تأكيد")
    {
        return System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new ConfirmationDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.Confirmed;
        });
    }

    public bool ShowDialog(object viewModel)
    {
        var viewName = viewModel.GetType().FullName!.Replace("ViewModel", "View");
        var viewType = Type.GetType(viewName);
        if (viewType != null)
        {
            if (Activator.CreateInstance(viewType) is Window window)
            {
                window.DataContext = viewModel;
                if (viewModel is SalesSystem.DesktopPWF.ViewModels.ViewModelBase vmBase)
                {
                    Action? closeHandler = null;
                    closeHandler = () =>
                    {
                        var dialogResultProp = viewModel.GetType().GetProperty("DialogResult");
                        if (dialogResultProp != null)
                        {
                            var dialogResult = dialogResultProp.GetValue(viewModel) as bool?;
                            if (dialogResult == true)
                            {
                                try { window.DialogResult = true; } catch { }
                            }
                        }
                        window.Close();
                    };
                    vmBase.CloseRequested += closeHandler;

                    window.Closed += (s, e) =>
                    {
                        if (closeHandler != null)
                        {
                            vmBase.CloseRequested -= closeHandler;
                        }
                    };
                }

                if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow != window)
                {
                    window.Owner = System.Windows.Application.Current.MainWindow;
                }
                var result = window.ShowDialog();
                var drProp = viewModel.GetType().GetProperty("DialogResult");
                if (drProp != null)
                {
                    return drProp.GetValue(viewModel) as bool? ?? false;
                }
                return result ?? false;
            }
        }
        return false;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new InfoDialog(message, title);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new ErrorDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

    public Task ShowSuccessAsync(string title, string message)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new SuccessDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new WarningDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new ConfirmationDialog(message);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.Confirmed;
        }).Task;
    }

    public Task<DeleteStrategy> ShowDeleteConfirmationAsync(string itemDescription)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new DeleteConfirmationDialog(itemDescription);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.SelectedStrategy;
        }).Task;
    }
}