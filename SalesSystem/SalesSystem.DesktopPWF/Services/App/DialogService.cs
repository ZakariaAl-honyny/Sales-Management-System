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
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
    }

    public void ShowWarning(string message, string title = "تنبيه")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
    }

    public void ShowError(string message, string title = "خطأ")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
    }

    public bool ShowConfirmation(string message, string title = "تأكيد")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        return result == MessageBoxResult.Yes;
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

                if (Application.Current.MainWindow != null && Application.Current.MainWindow != window)
                {
                    window.Owner = Application.Current.MainWindow;
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

    public Task ShowErrorAsync(string title, string message)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new ErrorDialog(message);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

    public Task ShowSuccessAsync(string title, string message)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new SuccessDialog(message);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new WarningDialog(message);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }).Task;
    }

public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new ConfirmationDialog(message);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.Confirmed;
        }).Task;
    }

    public Task<DeleteStrategy> ShowDeleteConfirmationAsync(string itemDescription)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new DeleteConfirmationDialog(itemDescription);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
            return dialog.SelectedStrategy;
        }).Task;
    }
}