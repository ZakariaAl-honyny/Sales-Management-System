using System.Windows;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.InventoryAdjustment;

namespace SalesSystem.DesktopPWF.Views.InventoryAdjustment;

/// <summary>
/// Interaction logic for InventoryAdjustmentListView.xaml
/// </summary>
public partial class InventoryAdjustmentListView : System.Windows.Controls.UserControl
{
    private readonly InventoryAdjustmentListViewModel _viewModel;

    public InventoryAdjustmentListView()
    {
        InitializeComponent();
        _viewModel = new InventoryAdjustmentListViewModel();
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAdjustmentsAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void AdjustmentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.EditAdjustmentFromDoubleClick();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}
