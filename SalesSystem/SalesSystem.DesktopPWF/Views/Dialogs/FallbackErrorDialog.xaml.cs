using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class FallbackErrorDialog : Window
{
    public FallbackErrorDialog(string message)
    {
        InitializeComponent();
        DataContext = message;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
}
