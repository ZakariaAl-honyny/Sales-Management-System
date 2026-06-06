using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;

namespace SalesSystem.DesktopPWF.Views.Suppliers;

/// <summary>
/// Interaction logic for SuppliersListView.xaml
/// </summary>
public partial class SuppliersListView : System.Windows.Controls.UserControl
{
    private readonly SupplierListViewModel _viewModel;

    public SuppliersListView()
    {
        InitializeComponent();
        _viewModel = new SupplierListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadSuppliersAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void SuppliersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.EditSupplierFromDoubleClick();
    }

    private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}


