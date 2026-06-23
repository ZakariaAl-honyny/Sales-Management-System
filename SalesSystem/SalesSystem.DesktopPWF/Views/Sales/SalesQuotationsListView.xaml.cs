using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Sales;

namespace SalesSystem.DesktopPWF.Views.Sales;

/// <summary>
/// Interaction logic for SalesQuotationsListView.xaml
/// </summary>
public partial class SalesQuotationsListView : UserControl
{
    private SalesQuotationListViewModel? _viewModel;

    public SalesQuotationsListView()
    {
        InitializeComponent();
        _viewModel = new SalesQuotationListViewModel();
        DataContext = _viewModel;
        
        Unloaded += (s, e) => _viewModel?.Cleanup();
    }
}
