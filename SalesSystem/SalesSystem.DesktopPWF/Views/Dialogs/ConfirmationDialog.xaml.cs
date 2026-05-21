using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class ConfirmationDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmationDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        Owner = Application.Current.MainWindow;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }
}