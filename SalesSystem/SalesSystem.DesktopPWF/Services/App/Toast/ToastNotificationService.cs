using System.Windows;
using System.Windows.Threading;

namespace SalesSystem.DesktopPWF.Services.App.Toast;

public enum ToastType { Success, Error, Info }

public interface IToastNotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowInfo(string message);
}

public class ToastNotificationService : IToastNotificationService
{
    private static readonly object _lock = new();
    private static readonly List<Window> _activeToasts = new();

    public void ShowSuccess(string message) => ShowToast(message, ToastType.Success, TimeSpan.FromSeconds(3));
    public void ShowError(string message) => ShowToast(message, ToastType.Error, TimeSpan.FromSeconds(5));
    public void ShowInfo(string message) => ShowToast(message, ToastType.Info, TimeSpan.FromSeconds(3));

    private void ShowToast(string message, ToastType type, TimeSpan duration)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            lock (_lock)
            {
                foreach (var t in _activeToasts.ToList())
                {
                    if (t is ToastWindow tw)
                    {
                        tw.CloseToast();
                    }
                }
            }

            var window = new ToastWindow(message, type, duration);
            window.Closed += (s, e) =>
            {
                lock (_lock)
                {
                    _activeToasts.Remove(window);
                }
            };

            lock (_lock)
            {
                _activeToasts.Add(window);
            }

            window.Show();
        });
    }
}