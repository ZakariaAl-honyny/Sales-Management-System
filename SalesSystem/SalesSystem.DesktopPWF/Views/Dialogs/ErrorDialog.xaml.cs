using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class ErrorDialog : Window
{
    public ErrorDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}