using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;

namespace SalesSystem.DesktopPWF.Views.CashBoxes;

public partial class CashBoxTransactionsView : UserControl
{
    private readonly CashBoxTransactionsViewModel _viewModel;

    public CashBoxTransactionsView()
    {
        InitializeComponent();

        _viewModel = new CashBoxTransactionsViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }
}
