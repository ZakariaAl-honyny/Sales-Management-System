using System.Windows;
using SalesSystem.DesktopPWF.Services.Api;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class DatabaseErrorDialog : Window
{
    public bool RetryClicked { get; private set; }
    private readonly Func<Task<HealthCheckResult>> _retryCheck;

    public DatabaseErrorDialog(string message, Func<Task<HealthCheckResult>> retryCheck)
    {
        InitializeComponent();
        MessageText.Text = message;
        _retryCheck = retryCheck;
        PositionOverOwner();
    }

    private void PositionOverOwner()
    {
        Owner = System.Windows.Application.Current.MainWindow;
        if (Owner != null)
        {
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
            Left = Owner.Left;
            Top = Owner.Top;
        }
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        ShowRetryingState();

        var result = await _retryCheck();

        if (result.IsDatabaseConnected)
        {
            RetryClicked = true;
            Close();
            return;
        }

        ShowErrorState(result.ErrorMessage ?? "تعذر الاتصال بقاعدة البيانات");
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        RetryClicked = false;
        Close();
    }

    private void ShowRetryingState()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        ErrorContentPanel.Visibility = Visibility.Collapsed;
        ErrorButtonsPanel.Visibility = Visibility.Collapsed;

        RetryingPanel.Visibility = Visibility.Visible;
        RetryingContentPanel.Visibility = Visibility.Visible;
        RetryingButtonsPanel.Visibility = Visibility.Visible;

        RetryingStatusText.Text = "جاري الاتصال بقاعدة البيانات...";
    }

    private void ShowErrorState(string message)
    {
        RetryingPanel.Visibility = Visibility.Collapsed;
        RetryingContentPanel.Visibility = Visibility.Collapsed;
        RetryingButtonsPanel.Visibility = Visibility.Collapsed;

        ErrorPanel.Visibility = Visibility.Visible;
        ErrorContentPanel.Visibility = Visibility.Visible;
        ErrorButtonsPanel.Visibility = Visibility.Visible;

        MessageText.Text = message;
    }
}
