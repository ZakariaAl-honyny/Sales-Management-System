using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesSystem.DesktopPWF.ViewModels.Sales;

namespace SalesSystem.DesktopPWF.Views.Sales;

/// <summary>
/// Interaction logic for SalesInvoicesListView.xaml
/// </summary>
public partial class SalesInvoicesListView : UserControl
{
    private SalesInvoiceListViewModel? _viewModel;

    public SalesInvoicesListView()
    {
        InitializeComponent();
        _viewModel = new SalesInvoiceListViewModel();
        DataContext = _viewModel;
        
        Unloaded += (s, e) => _viewModel?.Cleanup();
    }

}

