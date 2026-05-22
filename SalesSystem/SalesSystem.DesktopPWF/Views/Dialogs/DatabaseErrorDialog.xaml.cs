using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

/// <summary>
/// Interaction logic for DatabaseErrorDialog.xaml
/// Shows when the application cannot connect to the database, with Retry and Exit options.
/// Returns true if user clicked Retry, false if Exit.
/// </summary>
public partial class DatabaseErrorDialog : Window
{
    public bool RetryClicked { get; private set; }

    public DatabaseErrorDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Owner = System.Windows.Application.Current?.MainWindow;
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        RetryClicked = true;
        Close();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        RetryClicked = false;
        Close();
    }
}
