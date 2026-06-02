using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;

namespace SalesSystem.DesktopPWF.Views.CashBoxes;

public partial class CashBoxesListView : UserControl
{
    private readonly CashBoxesListViewModel _viewModel;

    public CashBoxesListView()
    {
        InitializeComponent();

        _viewModel = new CashBoxesListViewModel();
        DataContext = _viewModel;
        _viewModel.OnNavigatedTo();

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
