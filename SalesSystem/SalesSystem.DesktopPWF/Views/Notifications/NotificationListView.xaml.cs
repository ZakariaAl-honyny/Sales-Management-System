using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Notifications;

namespace SalesSystem.DesktopPWF.Views.Notifications;

/// <summary>
/// Interaction logic for NotificationListView.xaml
/// </summary>
public partial class NotificationListView : System.Windows.Controls.UserControl
{
    private readonly NotificationListViewModel _viewModel;

    public NotificationListView()
    {
        InitializeComponent();
        _viewModel = new NotificationListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadNotificationsAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private async void NotificationsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        await _viewModel.MarkAsReadAsync();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}
