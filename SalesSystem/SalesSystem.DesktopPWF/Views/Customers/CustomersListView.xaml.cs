using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Customers;

namespace SalesSystem.DesktopPWF.Views.Customers;

/// <summary>
/// Interaction logic for CustomersListView.xaml
/// </summary>
public partial class CustomersListView : System.Windows.Controls.UserControl
{
    private readonly CustomerListViewModel _viewModel;

    public CustomersListView()
    {
        InitializeComponent();
        _viewModel = new CustomerListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCustomersAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.EditCustomerFromDoubleClick();
    }

    private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}

