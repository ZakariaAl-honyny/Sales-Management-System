using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Transfers;

namespace SalesSystem.DesktopPWF.Views.Transfers;

public partial class WarehouseTransfersListView : UserControl
{
    private WarehouseTransfersListViewModel _viewModel = null!;

    public WarehouseTransfersListView()
    {
        InitializeComponent();
        _viewModel = new WarehouseTransfersListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

}

