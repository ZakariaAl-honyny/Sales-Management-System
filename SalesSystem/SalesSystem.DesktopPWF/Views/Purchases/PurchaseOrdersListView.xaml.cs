using System.Windows.Controls;
using SalesSystem.DesktopPWF.ViewModels.Purchases;

namespace SalesSystem.DesktopPWF.Views.Purchases;

/// <summary>
/// Interaction logic for PurchaseOrdersListView.xaml
/// </summary>
public partial class PurchaseOrdersListView : UserControl
{
    public PurchaseOrdersListView()
    {
        InitializeComponent();
    }

    public PurchaseOrdersListView(PurchaseOrderListViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
