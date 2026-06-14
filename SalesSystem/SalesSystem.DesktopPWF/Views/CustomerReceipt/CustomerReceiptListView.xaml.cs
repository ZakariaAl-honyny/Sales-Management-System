using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.CustomerReceipt;

namespace SalesSystem.DesktopPWF.Views.CustomerReceipt;

/// <summary>
/// Interaction logic for CustomerReceiptListView.xaml
/// </summary>
public partial class CustomerReceiptListView : System.Windows.Controls.UserControl
{
    private readonly CustomerReceiptListViewModel _viewModel;

    public CustomerReceiptListView()
    {
        InitializeComponent();
        _viewModel = new CustomerReceiptListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadReceiptsAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void ReceiptsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.EditReceiptFromDoubleClick();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}
