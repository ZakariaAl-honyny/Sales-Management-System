using System.Collections.Generic;
using System.Windows;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class ValidationErrorsDialog : Window
{
    public ValidationErrorsDialog(string title, List<string> errors)
    {
        InitializeComponent();
        TitleText.Text = title;
        ErrorsList.ItemsSource = errors;
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

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
}
