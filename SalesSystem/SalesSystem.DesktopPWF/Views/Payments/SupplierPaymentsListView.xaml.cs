using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Payments;

namespace SalesSystem.DesktopPWF.Views.Payments;

public partial class SupplierPaymentsListView : UserControl
{
    private SupplierPaymentsListViewModel _viewModel = null!;

    public SupplierPaymentsListView()
    {
        InitializeComponent();
        _viewModel = new SupplierPaymentsListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedPayment != null)
        {
            _viewModel.ViewCommand.Execute(null);
        }
    }
}

