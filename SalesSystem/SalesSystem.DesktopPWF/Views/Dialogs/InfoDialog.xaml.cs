using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class InfoDialog : Window
{
    public InfoDialog(string message, string title = "معلومات")
    {
        InitializeComponent();
        MessageText.Text = message;
        TitleText.Text = title;
        Title = title;
        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
