using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.InventoryCount;

namespace SalesSystem.DesktopPWF.Views.InventoryCount;

/// <summary>
/// Interaction logic for InventoryCountListView.xaml
/// </summary>
public partial class InventoryCountListView : System.Windows.Controls.UserControl
{
    private readonly InventoryCountListViewModel _viewModel;

    public InventoryCountListView()
    {
        InitializeComponent();
        _viewModel = new InventoryCountListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCountsAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void CountsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.EditCountFromDoubleClick();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}
