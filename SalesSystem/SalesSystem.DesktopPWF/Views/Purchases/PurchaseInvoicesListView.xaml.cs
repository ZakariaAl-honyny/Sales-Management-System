using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Purchases;

namespace SalesSystem.DesktopPWF.Views.Purchases;

/// <summary>
/// Interaction logic for PurchaseInvoicesListView.xaml
/// </summary>
public partial class PurchaseInvoicesListView : UserControl
{
    private PurchaseInvoiceListViewModel? _viewModel;

    public PurchaseInvoicesListView()
    {
        InitializeComponent();
        _viewModel = new PurchaseInvoiceListViewModel();
        DataContext = _viewModel;

        Unloaded += (s, e) => _viewModel.Cleanup();
    }

}

