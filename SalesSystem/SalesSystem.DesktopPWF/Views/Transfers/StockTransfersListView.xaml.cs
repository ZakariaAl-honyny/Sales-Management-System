using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Transfers;

namespace SalesSystem.DesktopPWF.Views.Transfers;

public partial class StockTransfersListView : Page
{
    private StockTransfersListViewModel _viewModel = null!;

    public StockTransfersListView()
    {
        InitializeComponent();
        _viewModel = new StockTransfersListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

}

