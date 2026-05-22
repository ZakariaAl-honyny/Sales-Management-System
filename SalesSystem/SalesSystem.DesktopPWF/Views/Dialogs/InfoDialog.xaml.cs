using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class InfoDialog : Window
{
    public InfoDialog(string message, string title = "معلومات")
    {
        InitializeComponent();
        MessageText.Text = message;
        TitleText.Text = title;
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

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
