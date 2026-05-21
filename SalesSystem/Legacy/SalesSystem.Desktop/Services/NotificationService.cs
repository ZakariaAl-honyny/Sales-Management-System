using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;

namespace SalesSystem.Desktop.Services;

public sealed class NotificationService : INotificationService
{
    public void ShowSuccess(string message) => Show(message, NotificationType.Success);
    public void ShowError(string message) => Show(message, NotificationType.Error);
    public void ShowWarning(string message) => Show(message, NotificationType.Warning);

    private void Show(string message, NotificationType type)
    {
        var notification = new Notification { Message = message, Type = type };
        var mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;

        if (mainForm != null && mainForm.InvokeRequired)
        {
            mainForm.Invoke(() => new ToastForm(notification).Show(mainForm));
        }
        else
        {
            new ToastForm(notification).Show(mainForm!);
        }
    }
}

