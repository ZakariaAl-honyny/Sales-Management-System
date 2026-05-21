using System.Windows;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Views.Dialogs;

public partial class DeleteConfirmationDialog : Window
{
    public DeleteStrategy SelectedStrategy { get; private set; } = DeleteStrategy.Cancel;

    public DeleteConfirmationDialog(string itemName)
    {
        InitializeComponent();
        ItemNameText.Text = itemName;
        Owner = Application.Current.MainWindow;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedStrategy = DeleteStrategy.Cancel;
        DialogResult = false;
        Close();
    }

    private void DeactivateButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedStrategy = DeleteStrategy.Deactivate;
        DialogResult = true;
        Close();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedStrategy = DeleteStrategy.Permanent;
        DialogResult = true;
        Close();
    }
}