using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Warehouses;

namespace SalesSystem.DesktopPWF.Views.Warehouses;

public partial class InventoryBatchesView : UserControl
{
    private readonly InventoryBatchesViewModel _viewModel;

    public InventoryBatchesView()
    {
        InitializeComponent();

        _viewModel = new InventoryBatchesViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
