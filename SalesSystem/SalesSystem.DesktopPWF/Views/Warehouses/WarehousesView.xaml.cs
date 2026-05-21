using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.ViewModels;

namespace SalesSystem.DesktopPWF.Views;

/// <summary>
/// Interaction logic for WarehousesView.xaml
/// </summary>
public partial class WarehousesView : Page
{
    private readonly WarehouseListViewModel _viewModel;

    public WarehousesView()
    {
        InitializeComponent();

        _viewModel = new WarehouseListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

    private void WarehousesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element &&
            element.DataContext is WarehouseDto)
        {
            _viewModel.EditWarehouseFromDoubleClick();
        }
    }
}

